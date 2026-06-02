using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Hpn.IntegrationTests.Identity;
using Hpn.Modules.Identity.Internal.Email;
using Hpn.Modules.Profile.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Xunit;

namespace Hpn.IntegrationTests.Profile;

/// <summary>
/// M2 profile flow over real Postgres: create/edit/activate and public visibility
/// projection checks.
/// </summary>
public sealed class ProfileFlowTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();
    private readonly CapturingEmailSender _emails = new();
    private WebApplicationFactory<Program> _factory = null!;

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync(TestContext.Current.CancellationToken);
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:Postgres", _postgres.GetConnectionString());
            builder.UseSetting("DevelopmentSeed:Enabled", "false");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IEmailSender>();
                services.RemoveAll<IProfileActivationRequirement>();
                services.AddSingleton<IEmailSender>(_emails);
                services.AddSingleton<IProfileActivationRequirement, AlwaysSatisfiedActivationRequirement>();
            });
        });
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Create_edit_interests_and_activate_profile()
    {
        var client = await SignInAsync("profile-owner@example.com");
        var ct = TestContext.Current.CancellationToken;

        var interestsResponse = await client.GetAsync("/api/v1/interests", ct);
        interestsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var interestIds = await ReadInterestIdsAsync(interestsResponse, ct);
        interestIds.Should().HaveCountGreaterThanOrEqualTo(3);

        var created = await client.PutAsJsonAsync("/api/v1/profile", new
        {
            displayName = "  Rowan  ",
            gender = "self_describe",
            selfDescribeText = "  genderqueer  ",
            countryCode = "ro",
            bio = "A person who notices practical kindness.",
        }, ct);
        created.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var doc = await ReadJsonAsync(created, ct))
        {
            doc.RootElement.GetProperty("displayName").GetString().Should().Be("Rowan");
            doc.RootElement.GetProperty("gender").GetString().Should().Be("self_describe");
            doc.RootElement.GetProperty("selfDescribeText").GetString().Should().Be("genderqueer");
            doc.RootElement.GetProperty("countryCode").GetString().Should().Be("RO");
            doc.RootElement.GetProperty("status").GetString().Should().Be("draft");
            var visibility = doc.RootElement.GetProperty("visibilityPreferences");
            visibility.GetProperty("showOnlyOutsideCountry").GetBoolean().Should().BeFalse();
            visibility.GetProperty("hideFromCountry").GetBoolean().Should().BeFalse();
            visibility.GetProperty("womenForWomen").GetBoolean().Should().BeFalse();
            visibility.GetProperty("verifiedOnly").GetBoolean().Should().BeFalse();
            visibility.GetProperty("paused").GetBoolean().Should().BeFalse();
        }

        var selected = interestIds.Take(2).ToArray();
        var interestsUpdated = await client.PutAsJsonAsync("/api/v1/profile/interests", new
        {
            interestIds = selected,
        }, ct);
        interestsUpdated.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var doc = await ReadJsonAsync(interestsUpdated, ct))
        {
            doc.RootElement.GetProperty("interests").EnumerateArray().Should().HaveCount(2);
        }

        var activated = await client.PutAsJsonAsync("/api/v1/profile/status", new { status = "active" }, ct);
        activated.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var doc = await ReadJsonAsync(activated, ct))
        {
            doc.RootElement.GetProperty("status").GetString().Should().Be("active");
            doc.RootElement.GetProperty("visibilityPreferences").GetProperty("paused").GetBoolean().Should().BeFalse();
        }

        var edited = await client.PutAsJsonAsync("/api/v1/profile", new
        {
            displayName = "Rowan A.",
            gender = "woman",
            selfDescribeText = "not stored",
            countryCode = "us",
            bio = "Still here for appreciation, not scores.",
        }, ct);
        edited.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var doc = await ReadJsonAsync(edited, ct))
        {
            doc.RootElement.GetProperty("displayName").GetString().Should().Be("Rowan A.");
            doc.RootElement.GetProperty("gender").GetString().Should().Be("woman");
            doc.RootElement.GetProperty("selfDescribeText").ValueKind.Should().Be(JsonValueKind.Null);
            doc.RootElement.GetProperty("countryCode").GetString().Should().Be("US");
            doc.RootElement.GetProperty("status").GetString().Should().Be("active");
        }
    }

    [Fact]
    public async Task Public_projection_requires_visibility_and_hides_owner_fields()
    {
        var owner = await SignInAsync("visible-owner@example.com");
        var viewer = await SignInAsync("profile-viewer@example.com");
        var ct = TestContext.Current.CancellationToken;

        var created = await owner.PutAsJsonAsync("/api/v1/profile", new
        {
            displayName = "Mira",
            gender = "woman",
            selfDescribeText = (string?)null,
            countryCode = "RO",
            bio = "Here to be noticed for the small true things.",
        }, ct);
        created.StatusCode.Should().Be(HttpStatusCode.OK);
        var profileId = await ReadProfileIdAsync(created, ct);

        var draftFromViewer = await viewer.GetAsync($"/api/v1/profiles/{profileId}", ct);
        draftFromViewer.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var activated = await owner.PutAsJsonAsync("/api/v1/profile/status", new { status = "active" }, ct);
        activated.StatusCode.Should().Be(HttpStatusCode.OK);

        var publicProjection = await viewer.GetAsync($"/api/v1/profiles/{profileId}", ct);
        publicProjection.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await publicProjection.Content.ReadAsStringAsync(ct);

        body.Should().Contain("\"displayName\":\"Mira\"");
        body.Should().NotContain("userId");
        body.Should().NotContain("status");
        body.Should().NotContain("visibilityPreferences");
    }

    private async Task<HttpClient> SignInAsync(string email)
    {
        var client = _factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        var requested = await client.PostAsJsonAsync("/api/v1/auth/magic-link", new { email }, ct);
        requested.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var token = _emails.LastTokenFor(email);
        token.Should().NotBeNullOrEmpty();

        var verified = await client.PostAsJsonAsync("/api/v1/auth/verify", new { token }, ct);
        verified.StatusCode.Should().Be(HttpStatusCode.OK);

        var cookie = ExtractSessionCookie(verified);
        cookie.Should().NotBeNull();
        client.DefaultRequestHeaders.Add("Cookie", cookie);
        return client;
    }

    private static async Task<IReadOnlyCollection<Guid>> ReadInterestIdsAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        using var doc = await ReadJsonAsync(response, cancellationToken);
        return doc.RootElement
            .EnumerateArray()
            .Select(e => e.GetProperty("id").GetGuid())
            .ToArray();
    }

    private static async Task<Guid> ReadProfileIdAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        using var doc = await ReadJsonAsync(response, cancellationToken);
        return doc.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response, CancellationToken cancellationToken) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

    private static string? ExtractSessionCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            return null;
        }

        var header = cookies.FirstOrDefault(c => c.StartsWith("hpn_session=", StringComparison.Ordinal));
        return header?.Split(';', 2)[0];
    }

    private sealed class AlwaysSatisfiedActivationRequirement : IProfileActivationRequirement
    {
        public Task<ProfileActivationRequirementResult> CheckAsync(
            Guid profileId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ProfileActivationRequirementResult.Pass);
    }
}
