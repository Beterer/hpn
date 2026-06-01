using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Hpn.Modules.Identity.Internal.Domain;
using Hpn.Modules.Identity.Internal.Email;
using Hpn.Modules.Identity.Internal.Persistence;
using Hpn.Modules.Identity.Internal.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Xunit;

namespace Hpn.IntegrationTests.Identity;

/// <summary>
/// End-to-end magic-link auth over a real Postgres: request → verify → /me →
/// logout, plus the expired and reused-token rejection paths (backbone §10.1,
/// MILESTONES M1). Email delivery is captured so the test can read the link.
/// </summary>
public sealed class AuthFlowTests : IAsyncLifetime
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
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IEmailSender>();
                services.AddSingleton<IEmailSender>(_emails);
            });
        });
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Request_verify_me_logout_round_trip()
    {
        const string email = "newcomer@example.com";
        var client = _factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        // 1. Request a link — always 202.
        var requested = await client.PostAsJsonAsync("/api/v1/auth/magic-link", new { email }, ct);
        requested.StatusCode.Should().Be(HttpStatusCode.Accepted);

        // 2. The link was "sent"; pull the token out of it.
        var token = _emails.LastTokenFor(email);
        token.Should().NotBeNullOrEmpty();

        // 3. Verify — sets the session cookie and returns the user.
        var verified = await client.PostAsJsonAsync("/api/v1/auth/verify", new { token }, ct);
        verified.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadEmail(verified, ct)).Should().Be(email);

        var cookie = ExtractSessionCookie(verified);
        cookie.Should().NotBeNull();

        // 4. /me with the cookie returns the account + onboarding state.
        using var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/me");
        meRequest.Headers.Add("Cookie", cookie);
        var me = await client.SendAsync(meRequest, ct);
        me.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var doc = JsonDocument.Parse(await me.Content.ReadAsStringAsync(ct)))
        {
            doc.RootElement.GetProperty("user").GetProperty("email").GetString().Should().Be(email);
            doc.RootElement.GetProperty("onboarding").GetProperty("nextStep").GetString().Should().Be("create_profile");
        }

        // 5. Logout revokes the session.
        using var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        logoutRequest.Headers.Add("Cookie", cookie);
        var loggedOut = await client.SendAsync(logoutRequest, ct);
        loggedOut.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 6. The revoked cookie no longer authenticates.
        using var afterLogout = new HttpRequestMessage(HttpMethod.Get, "/api/v1/me");
        afterLogout.Headers.Add("Cookie", cookie);
        var meAgain = await client.SendAsync(afterLogout, ct);
        meAgain.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_without_a_session_is_unauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/me", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Reused_token_is_rejected_on_second_verify()
    {
        const string email = "reuser@example.com";
        var client = _factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        await client.PostAsJsonAsync("/api/v1/auth/magic-link", new { email }, ct);
        var token = _emails.LastTokenFor(email);

        var first = await client.PostAsJsonAsync("/api/v1/auth/verify", new { token }, ct);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await client.PostAsJsonAsync("/api/v1/auth/verify", new { token }, ct);
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Expired_token_is_rejected()
    {
        const string email = "stale@example.com";
        var rawToken = SecureTokenGenerator.Generate();
        var ct = TestContext.Current.CancellationToken;

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var issuedAt = DateTimeOffset.UtcNow.AddHours(-1);
            var user = User.Register(email, issuedAt);
            db.Users.Add(user);
            db.MagicLinkTokens.Add(MagicLinkToken.Issue(
                user.Id, TokenHasher.Hash(rawToken), issuedAt, TimeSpan.FromMinutes(15), requestedIp: null));
            await db.SaveChangesAsync(ct);
        }

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/verify", new { token = rawToken }, ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static async Task<string?> ReadEmail(HttpResponseMessage response, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        return doc.RootElement.GetProperty("email").GetString();
    }

    private static string? ExtractSessionCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            return null;
        }

        var header = cookies.FirstOrDefault(c => c.StartsWith("hpn_session=", StringComparison.Ordinal));
        if (header is null)
        {
            return null;
        }

        // Keep just the name=value pair to send back as the request Cookie.
        return header.Split(';', 2)[0];
    }
}
