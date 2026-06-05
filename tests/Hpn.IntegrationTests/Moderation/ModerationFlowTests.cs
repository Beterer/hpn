using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Hpn.IntegrationTests.Identity;
using Hpn.Modules.Identity.Internal.Email;
using Hpn.Modules.Moderation.Internal.Actions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Hpn.IntegrationTests.Moderation;

/// <summary>
/// M9 reporting &amp; moderation over real Postgres (backbone §6.7, §10.3): report
/// intake and de-duplication, weighted report pressure auto-applying a temporary
/// restriction (never a ban) that drops the account from the feed, automatic release
/// when the window elapses, and trusted-vs-low-trust reporters carrying different
/// weight.
/// </summary>
public sealed class ModerationFlowTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();
    private readonly CapturingEmailSender _emails = new();
    private WebApplicationFactory<Program> _factory = null!;

    private CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync(Ct);
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:Postgres", _postgres.GetConnectionString());
            builder.UseSetting("DevelopmentSeed:Enabled", "false");
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

    // ---- intake + dedup ----------------------------------------------------

    [Fact]
    public async Task Reports_are_accepted_and_deduplicated_per_reporter_target_type()
    {
        var reporter = await CreateActiveParticipantAsync("rep-a@example.com");
        var target = await CreateActiveParticipantAsync("rep-a-target@example.com");

        var first = await ReportAsync(reporter.Client, target.ProfileId, "spam");
        first.StatusCode.Should().Be(HttpStatusCode.Accepted);

        // Same reporter/target/type collapses — still 202, but no second row.
        var duplicate = await ReportAsync(reporter.Client, target.ProfileId, "spam");
        duplicate.StatusCode.Should().Be(HttpStatusCode.Accepted);

        // A different type from the same reporter is a distinct report.
        var otherType = await ReportAsync(reporter.Client, target.ProfileId, "harassment");
        otherType.StatusCode.Should().Be(HttpStatusCode.Accepted);

        (await CountAsync(
            "SELECT count(*) FROM moderation.reports WHERE reporter_user_id = @a AND target_profile_id = @b",
            ("a", reporter.UserId), ("b", target.ProfileId)))
            .Should().Be(2);
    }

    [Fact]
    public async Task Cannot_report_yourself()
    {
        var reporter = await CreateActiveParticipantAsync("rep-self@example.com");

        var response = await ReportAsync(reporter.Client, reporter.ProfileId, "spam");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---- weighted pressure → temporary restriction -------------------------

    [Fact]
    public async Task Three_trusted_reporters_trip_a_temporary_restriction_not_a_ban_and_drop_the_target_from_the_feed()
    {
        var observer = await CreateActiveParticipantAsync("mod-observer@example.com");
        var target = await CreateActiveParticipantAsync("mod-target@example.com");

        var reporters = new List<Participant>();
        for (var i = 0; i < 3; i++)
        {
            var reporter = await CreateActiveParticipantAsync($"mod-reporter-{i}@example.com");
            await BoostTrustAsync(reporter); // verified + aged → high weight
            reporters.Add(reporter);
        }

        (await GetFeedAsync(observer.Client)).Should().Contain(target.ProfileId);

        foreach (var reporter in reporters)
        {
            (await ReportAsync(reporter.Client, target.ProfileId, "harassment"))
                .StatusCode.Should().Be(HttpStatusCode.Accepted);
        }

        // The account is restricted, not banned (§10.3).
        (await StatusAsync(target.ProfileId)).Should().Be("under_review");
        (await CountAsync(
            "SELECT count(*) FROM moderation.moderation_actions WHERE target_user_id = @id AND action = 'temp_restrict'",
            ("id", target.UserId))).Should().Be(1);
        (await CountAsync(
            "SELECT count(*) FROM moderation.moderation_actions WHERE target_user_id = @id AND action = 'ban'",
            ("id", target.UserId))).Should().Be(0);

        // …and it has left everyone's feed.
        (await GetFeedAsync(observer.Client)).Should().NotContain(target.ProfileId);

        // The reports that drove it are now in the review queue.
        (await CountAsync(
            "SELECT count(*) FROM moderation.reports WHERE target_profile_id = @id AND status = 'reviewing'",
            ("id", target.ProfileId))).Should().Be(3);
    }

    [Fact]
    public async Task A_lapsed_restriction_is_released_and_the_account_returns_to_the_feed()
    {
        var observer = await CreateActiveParticipantAsync("exp-observer@example.com");
        var target = await CreateActiveParticipantAsync("exp-target@example.com");

        for (var i = 0; i < 3; i++)
        {
            var reporter = await CreateActiveParticipantAsync($"exp-reporter-{i}@example.com");
            await BoostTrustAsync(reporter);
            (await ReportAsync(reporter.Client, target.ProfileId, "spam"))
                .StatusCode.Should().Be(HttpStatusCode.Accepted);
        }

        (await StatusAsync(target.ProfileId)).Should().Be("under_review");
        (await GetFeedAsync(observer.Client)).Should().NotContain(target.ProfileId);

        // The gated maintenance step, run with the 48h window elapsed (no worker, §12).
        using (var scope = _factory.Services.CreateScope())
        {
            var expiry = scope.ServiceProvider.GetRequiredService<RestrictionExpiryService>();
            var released = await expiry.ReleaseExpiredAsync(DateTimeOffset.UtcNow.AddHours(49), Ct);
            released.Should().Be(1);
        }

        (await StatusAsync(target.ProfileId)).Should().Be("active");
        (await CountAsync(
            "SELECT count(*) FROM moderation.moderation_actions WHERE target_user_id = @id AND action = 'clear'",
            ("id", target.UserId))).Should().Be(1);
        (await GetFeedAsync(observer.Client)).Should().Contain(target.ProfileId);
    }

    [Fact]
    public async Task Low_trust_reporters_do_not_trip_a_restriction()
    {
        var observer = await CreateActiveParticipantAsync("low-observer@example.com");
        var target = await CreateActiveParticipantAsync("low-target@example.com");

        // Three reporters meet the distinct-reporter floor, but at default (low) trust
        // their combined weighted pressure stays under the threshold (§10.3).
        for (var i = 0; i < 3; i++)
        {
            var reporter = await CreateActiveParticipantAsync($"low-reporter-{i}@example.com");
            (await ReportAsync(reporter.Client, target.ProfileId, "spam"))
                .StatusCode.Should().Be(HttpStatusCode.Accepted);
        }

        (await StatusAsync(target.ProfileId)).Should().Be("active", "weighted pressure stayed under the threshold");
        (await CountAsync(
            "SELECT count(*) FROM moderation.moderation_actions WHERE target_user_id = @id",
            ("id", target.UserId))).Should().Be(0);
        (await GetFeedAsync(observer.Client)).Should().Contain(target.ProfileId);
    }

    // ---- helpers -----------------------------------------------------------

    private Task<HttpResponseMessage> ReportAsync(HttpClient client, Guid targetProfileId, string type) =>
        client.PostAsJsonAsync("/api/v1/reports", new { targetProfileId, type, note = (string?)null }, Ct);

    private async Task<string?> StatusAsync(Guid profileId)
    {
        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync(Ct);
        await using var command = new NpgsqlCommand(
            "SELECT status FROM profile.profiles WHERE id = @id", connection);
        command.Parameters.AddWithValue("id", profileId);
        return (string?)await command.ExecuteScalarAsync(Ct);
    }

    /// <summary>Backdates the account and verifies the profile so its recomputed trust
    /// is high (aged + verified + photo), making it a heavily-weighted reporter.</summary>
    private async Task BoostTrustAsync(Participant participant)
    {
        await ExecuteAsync(
            "UPDATE identity.users SET created_at = now() - interval '30 days' WHERE id = @id",
            p => p.AddWithValue("id", participant.UserId));
        await ExecuteAsync(
            "UPDATE profile.profiles SET verified = true WHERE id = @id",
            p => p.AddWithValue("id", participant.ProfileId));
    }

    private async Task<IReadOnlyList<Guid>> GetFeedAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/feed/next?limit=20", Ct);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        return doc.RootElement.EnumerateArray().Select(e => e.GetProperty("profileId").GetGuid()).ToArray();
    }

    private sealed record Participant(HttpClient Client, Guid ProfileId, Guid UserId);

    private async Task<Participant> CreateActiveParticipantAsync(string email)
    {
        var client = await SignInAsync(email);
        var created = await client.PutAsJsonAsync("/api/v1/profile", new
        {
            displayName = email.Split('@')[0],
            gender = "woman",
            selfDescribeText = (string?)null,
        }, Ct);
        created.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await created.Content.ReadAsStringAsync(Ct));
        var profileId = doc.RootElement.GetProperty("id").GetGuid();
        var userId = await ScalarAsync<Guid>("SELECT user_id FROM profile.profiles WHERE id = @id", profileId);

        await InsertReadyPhotoAsync(profileId);
        await ExecuteAsync(
            "UPDATE profile.profiles SET status = 'active', updated_at = now() WHERE id = @id",
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

        var header = verified.Headers.GetValues("Set-Cookie")
            .First(c => c.StartsWith("hpn_session=", StringComparison.Ordinal));
        client.DefaultRequestHeaders.Add("Cookie", header.Split(';', 2)[0]);
        return client;
    }

    private async Task InsertReadyPhotoAsync(Guid profileId)
    {
        var photoId = Guid.NewGuid();
        var prefix = $"profiles/{profileId}/photos/{photoId}";
        var contentHash = (photoId.ToString("N") + photoId.ToString("N"))[..64];
        await ExecuteAsync(
            "INSERT INTO photo.photos " +
            "(id, profile_id, position, status, original_key, display_key, thumb_key, width, height, content_hash, created_at) " +
            "VALUES (@id, @pid, 0, 'ready', @ok, @dk, @tk, 400, 400, @hash, now())",
            p =>
            {
                p.AddWithValue("id", photoId);
                p.AddWithValue("pid", profileId);
                p.AddWithValue("ok", $"{prefix}/original.webp");
                p.AddWithValue("dk", $"{prefix}/display.webp");
                p.AddWithValue("tk", $"{prefix}/thumb.webp");
                p.AddWithValue("hash", contentHash);
            });
    }

    private async Task ExecuteAsync(string sql, Action<NpgsqlParameterCollection> bind)
    {
        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync(Ct);
        await using var command = new NpgsqlCommand(sql, connection);
        bind(command.Parameters);
        await command.ExecuteNonQueryAsync(Ct);
    }

    private async Task<T> ScalarAsync<T>(string sql, Guid id)
    {
        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync(Ct);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);
        return (T)(await command.ExecuteScalarAsync(Ct))!;
    }

    private async Task<long> CountAsync(string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync(Ct);
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        return (long)(await command.ExecuteScalarAsync(Ct))!;
    }
}
