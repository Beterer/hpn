using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Hpn.IntegrationTests.Identity;
using Hpn.Modules.Identity.Internal.Email;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Hpn.IntegrationTests.SocialFingerprint;

/// <summary>
/// M7 over real Postgres: the gated social fingerprint read model, opportunistic
/// weekly snapshots, and private appreciation-style comparison.
/// </summary>
public sealed class SocialFingerprintFlowTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();
    private readonly CapturingEmailSender _emails = new();
    private WebApplicationFactory<Program> _factory = null!;

    private CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync(Ct);
        UseFactory();
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Fingerprint_is_gated_until_twenty_and_writes_one_weekly_snapshot()
    {
        var target = await CreateDraftParticipantAsync("fingerprint-target@example.com");
        var categories = await GetCategoriesAsync(target.Client);
        var warmSmile = categories.Single(c => c.Slug == "warm_smile");
        var creative = categories.Single(c => c.Slug == "creative");

        var gated = await target.Client.GetAsync("/api/v1/fingerprint/me", Ct);
        var gatedBody = await gated.Content.ReadAsStringAsync(Ct);
        gated.StatusCode.Should().Be(HttpStatusCode.OK, gatedBody);
        using (var gatedDoc = JsonDocument.Parse(gatedBody))
        {
            gatedDoc.RootElement.GetProperty("status").GetString().Should().Be("insufficient_data");
            gatedDoc.RootElement.GetProperty("needed").GetInt32().Should().Be(20);
            gatedDoc.RootElement.GetProperty("sampleSize").GetInt32().Should().Be(0);
        }

        await InsertReceivedStatAsync(target.ProfileId, warmSmile.Id, count: 14);
        await InsertReceivedStatAsync(target.ProfileId, creative.Id, count: 6);

        var ready = await target.Client.GetAsync("/api/v1/fingerprint/me", Ct);
        ready.StatusCode.Should().Be(HttpStatusCode.OK);
        using var readyDoc = JsonDocument.Parse(await ready.Content.ReadAsStringAsync(Ct));
        var root = readyDoc.RootElement;

        root.GetProperty("status").GetString().Should().Be("ready");
        root.GetProperty("needed").GetInt32().Should().Be(0);
        root.GetProperty("profileId").GetGuid().Should().Be(target.ProfileId);
        root.GetProperty("headline").GetString().Should().StartWith("People often perceive");
        root.GetProperty("sampleSize").GetInt32().Should().Be(20);

        var distribution = root.GetProperty("distribution").EnumerateArray().ToArray();
        distribution.Should().HaveCount(12);
        distribution.Single(c => c.GetProperty("slug").GetString() == "warm_smile")
            .GetProperty("share").GetDouble().Should().Be(0.7);
        distribution.Single(c => c.GetProperty("slug").GetString() == "creative")
            .GetProperty("share").GetDouble().Should().Be(0.3);

        var topTraits = root.GetProperty("topTraits").EnumerateArray().ToArray();
        topTraits.Select(t => t.GetProperty("slug").GetString()).Take(2).Should().Equal("warm_smile", "creative");
        topTraits[0].GetProperty("phrasing").GetString().Should().Contain("perceive");

        var trend = root.GetProperty("trend").EnumerateArray().ToArray();
        trend.Should().ContainSingle();
        trend[0].GetProperty("sampleSize").GetInt32().Should().Be(20);

        var snapshotCount = await ScalarIntAsync(
            "SELECT count(*) FROM social_fingerprint.social_fingerprint_snapshots WHERE profile_id = @profile",
            p => p.AddWithValue("profile", target.ProfileId));
        snapshotCount.Should().Be(1);

        var secondRead = await target.Client.GetAsync("/api/v1/fingerprint/me", Ct);
        secondRead.StatusCode.Should().Be(HttpStatusCode.OK);
        snapshotCount = await ScalarIntAsync(
            "SELECT count(*) FROM social_fingerprint.social_fingerprint_snapshots WHERE profile_id = @profile",
            p => p.AddWithValue("profile", target.ProfileId));
        snapshotCount.Should().Be(1, "the weekly snapshot is inserted once per period");

        var body = root.GetRawText().ToLowerInvariant();
        body.Should().NotContain("score");
        body.Should().NotContain("rank");
        body.Should().NotContain("leaderboard");
        body.Should().NotContain("popular");
    }

    [Fact]
    public async Task Appreciation_style_compares_user_mix_to_platform_average_without_score_copy()
    {
        var viewer = await CreateDraftParticipantAsync("style-viewer@example.com");
        var other = await CreateDraftParticipantAsync("style-other@example.com");
        var categories = await GetCategoriesAsync(viewer.Client);
        var warmSmile = categories.Single(c => c.Slug == "warm_smile");
        var creative = categories.Single(c => c.Slug == "creative");
        var kind = categories.Single(c => c.Slug == "kind");

        await InsertGivenStatAsync(viewer.UserId, warmSmile.Id, count: 3);
        await InsertGivenStatAsync(viewer.UserId, creative.Id, count: 1);
        await InsertGivenStatAsync(other.UserId, creative.Id, count: 3);
        await InsertGivenStatAsync(other.UserId, kind.Id, count: 1);

        var response = await viewer.Client.GetAsync("/api/v1/appreciation-style/me", Ct);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        var root = doc.RootElement;

        root.GetProperty("status").GetString().Should().Be("ready");
        root.GetProperty("headline").GetString().Should().Be("What you tend to notice");
        root.GetProperty("total").GetInt32().Should().Be(4);

        var styleCategories = root.GetProperty("categories").EnumerateArray().ToArray();
        styleCategories.Should().HaveCount(12);
        var warm = styleCategories.Single(c => c.GetProperty("slug").GetString() == "warm_smile");
        warm.GetProperty("count").GetInt32().Should().Be(3);
        warm.GetProperty("share").GetDouble().Should().Be(0.75);
        warm.GetProperty("platformShare").GetDouble().Should().Be(0.375);
        warm.GetProperty("difference").GetDouble().Should().Be(0.375);
        warm.GetProperty("insight").GetString().Should().Contain("wider Notice pattern");

        var creativeStyle = styleCategories.Single(c => c.GetProperty("slug").GetString() == "creative");
        creativeStyle.GetProperty("share").GetDouble().Should().Be(0.25);
        creativeStyle.GetProperty("platformShare").GetDouble().Should().Be(0.5);
        creativeStyle.GetProperty("difference").GetDouble().Should().Be(-0.25);

        var body = root.GetRawText().ToLowerInvariant();
        body.Should().NotContain("score");
        body.Should().NotContain("rank");
        body.Should().NotContain("leaderboard");
        body.Should().NotContain("popular");
    }

    [Fact]
    public async Task Fingerprint_requires_a_profile()
    {
        var client = await SignInAsync("fingerprint-no-profile@example.com");

        var response = await client.GetAsync("/api/v1/fingerprint/me", Ct);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record Category(Guid Id, string Slug, string Label, int SortOrder);

    private async Task<Category[]> GetCategoriesAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/appreciation-categories", Ct);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var categories = await response.Content.ReadFromJsonAsync<Category[]>(cancellationToken: Ct);
        categories.Should().NotBeNull();
        categories.Should().HaveCount(12);
        return categories!;
    }

    private sealed record Participant(HttpClient Client, Guid ProfileId, Guid UserId);

    private async Task<Participant> CreateDraftParticipantAsync(string email)
    {
        var client = await SignInAsync(email);
        var created = await client.PutAsJsonAsync("/api/v1/profile", new
        {
            displayName = email.Split('@')[0],
            gender = "woman",
            selfDescribeText = (string?)null,
            countryCode = "RO",
            bio = "Here for appreciation, not scores.",
        }, Ct);
        created.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await created.Content.ReadAsStringAsync(Ct));
        var profileId = doc.RootElement.GetProperty("id").GetGuid();
        var userId = await ScalarGuidAsync(
            "SELECT user_id FROM profile.profiles WHERE id = @id",
            p => p.AddWithValue("id", profileId));
        return new Participant(client, profileId, userId);
    }

    private async Task<HttpClient> SignInAsync(string email)
    {
        var client = _factory.CreateClient();

        var requested = await client.PostAsJsonAsync("/api/v1/auth/magic-link", new { email }, Ct);
        requested.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var token = _emails.LastTokenFor(email);
        token.Should().NotBeNullOrEmpty();

        var verified = await client.PostAsJsonAsync("/api/v1/auth/verify", new { token }, Ct);
        verified.StatusCode.Should().Be(HttpStatusCode.OK);

        client.DefaultRequestHeaders.Add("Cookie", ExtractSessionCookie(verified));
        return client;
    }

    private Task InsertReceivedStatAsync(Guid profileId, Guid categoryId, int count) => ExecuteAsync(
        """
        INSERT INTO appreciation.received_appreciation_stats
            (receiver_profile_id, category_id, count, last_at)
        VALUES
            (@profile, @category, @count, now())
        """,
        p =>
        {
            p.AddWithValue("profile", profileId);
            p.AddWithValue("category", categoryId);
            p.AddWithValue("count", count);
        });

    private Task InsertGivenStatAsync(Guid senderUserId, Guid categoryId, int count) => ExecuteAsync(
        """
        INSERT INTO appreciation.given_appreciation_stats
            (sender_user_id, category_id, count)
        VALUES
            (@sender, @category, @count)
        """,
        p =>
        {
            p.AddWithValue("sender", senderUserId);
            p.AddWithValue("category", categoryId);
            p.AddWithValue("count", count);
        });

    private async Task ExecuteAsync(string sql, Action<NpgsqlParameterCollection> bind)
    {
        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync(Ct);
        await using var command = new NpgsqlCommand(sql, connection);
        bind(command.Parameters);
        await command.ExecuteNonQueryAsync(Ct);
    }

    private async Task<Guid> ScalarGuidAsync(string sql, Action<NpgsqlParameterCollection> bind)
    {
        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync(Ct);
        await using var command = new NpgsqlCommand(sql, connection);
        bind(command.Parameters);
        var result = await command.ExecuteScalarAsync(Ct);
        return (Guid)result!;
    }

    private async Task<int> ScalarIntAsync(string sql, Action<NpgsqlParameterCollection> bind)
    {
        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync(Ct);
        await using var command = new NpgsqlCommand(sql, connection);
        bind(command.Parameters);
        var result = await command.ExecuteScalarAsync(Ct);
        return Convert.ToInt32(result);
    }

    private void UseFactory()
    {
        _factory?.Dispose();
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

    private static string ExtractSessionCookie(HttpResponseMessage response)
    {
        var header = response.Headers.GetValues("Set-Cookie")
            .First(c => c.StartsWith("hpn_session=", StringComparison.Ordinal));
        return header.Split(';', 2)[0];
    }
}
