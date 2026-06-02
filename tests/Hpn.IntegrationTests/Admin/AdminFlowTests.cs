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

namespace Hpn.IntegrationTests.Admin;

/// <summary>
/// M10 admin tools over real Postgres: the internal surface is admin-only and all
/// operator decisions are mirrored into <c>admin.admin_audit_log</c> (§6.8, §11).
/// </summary>
public sealed class AdminFlowTests : IAsyncLifetime
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
    public async Task Admin_endpoints_reject_anonymous_and_non_admin_users()
    {
        var anonymous = _factory.CreateClient();
        var member = await SignInAsync("admin-member@example.com");

        (await anonymous.GetAsync("/api/v1/admin/stats", Ct)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await member.GetAsync("/api/v1/admin/stats", Ct)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_can_review_queue_apply_profile_action_and_audit_the_decision()
    {
        var admin = await CreateActiveParticipantAsync("admin-reviewer@example.com");
        var reporter = await CreateActiveParticipantAsync("admin-reporter@example.com");
        var target = await CreateActiveParticipantAsync("admin-target@example.com");
        await PromoteToAdminAsync(admin.UserId);

        (await ReportAsync(reporter.Client, target.ProfileId, "spam"))
            .StatusCode.Should().Be(HttpStatusCode.Accepted);

        using var queueDoc = await GetJsonAsync(admin.Client, "/api/v1/admin/queue");
        var queueItem = queueDoc.RootElement.EnumerateArray()
            .Single(i => i.GetProperty("profileId").GetGuid() == target.ProfileId);
        queueItem.GetProperty("reportCount").GetInt32().Should().Be(1);

        using var reportsDoc = await GetJsonAsync(admin.Client, "/api/v1/admin/reports");
        var report = reportsDoc.RootElement.EnumerateArray()
            .Single(r => r.GetProperty("targetProfileId").GetGuid() == target.ProfileId);
        report.GetProperty("status").GetString().Should().Be("open");

        var actionResponse = await admin.Client.PostAsJsonAsync(
            $"/api/v1/admin/profiles/{target.ProfileId}/action",
            new { action = "ban", reason = "Confirmed fake profile." },
            Ct);
        actionResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var actionDoc = JsonDocument.Parse(await actionResponse.Content.ReadAsStringAsync(Ct));
        var auditId = actionDoc.RootElement.GetProperty("auditId").GetGuid();
        auditId.Should().NotBeEmpty();
        actionDoc.RootElement.GetProperty("action").GetString().Should().Be("ban");

        (await ScalarAsync<string>("SELECT status FROM profile.profiles WHERE id = @id", target.ProfileId))
            .Should().Be("banned");
        (await CountAsync(
            "SELECT count(*) FROM moderation.moderation_actions WHERE target_user_id = @target AND action = 'ban' AND actor = @actor",
            ("target", target.UserId), ("actor", admin.UserId.ToString())))
            .Should().Be(1);
        (await CountAsync(
            "SELECT count(*) FROM moderation.reports WHERE target_profile_id = @target AND status = 'actioned'",
            ("target", target.ProfileId)))
            .Should().Be(1);
        (await CountAsync(
            "SELECT count(*) FROM admin.admin_audit_log WHERE id = @audit AND admin_user_id = @admin AND action = 'profile_action:ban' AND target_ref = @target",
            ("audit", auditId), ("admin", admin.UserId), ("target", $"profile:{target.ProfileId}")))
            .Should().Be(1);

        using var statsDoc = await GetJsonAsync(admin.Client, "/api/v1/admin/stats");
        statsDoc.RootElement.GetProperty("actionedReports").GetInt32().Should().BeGreaterThanOrEqualTo(1);
        statsDoc.RootElement.GetProperty("currentlyBanned").GetInt32().Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Admin_dismissed_appeal_is_audited_but_takes_no_action()
    {
        var admin = await CreateActiveParticipantAsync("admin-appeals@example.com");
        await PromoteToAdminAsync(admin.UserId);
        var target = await CreateActiveParticipantAsync("admin-appeal-target@example.com");
        var appealId = Guid.NewGuid();

        var response = await admin.Client.PostAsJsonAsync(
            $"/api/v1/admin/appeals/{appealId}/resolve",
            new { targetProfileId = target.ProfileId, outcome = "dismissed", note = "No new evidence was provided." },
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        var auditId = doc.RootElement.GetProperty("auditId").GetGuid();
        doc.RootElement.GetProperty("appealId").GetGuid().Should().Be(appealId);
        doc.RootElement.GetProperty("outcome").GetString().Should().Be("dismissed");
        doc.RootElement.GetProperty("restrictionLifted").GetBoolean().Should().BeFalse();

        (await CountAsync(
            "SELECT count(*) FROM admin.admin_audit_log WHERE id = @audit AND admin_user_id = @admin AND action = 'appeal.resolve' AND target_ref = @target",
            ("audit", auditId), ("admin", admin.UserId), ("target", $"appeal:{appealId}")))
            .Should().Be(1);
        // A dismissed appeal records the decision but never touches moderation state.
        (await CountAsync(
            "SELECT count(*) FROM moderation.moderation_actions WHERE target_user_id = @id",
            ("id", target.UserId)))
            .Should().Be(0);
    }

    [Fact]
    public async Task Admin_upheld_appeal_lifts_the_ban_and_returns_the_profile_to_active()
    {
        var admin = await CreateActiveParticipantAsync("admin-uphold@example.com");
        await PromoteToAdminAsync(admin.UserId);
        var target = await CreateActiveParticipantAsync("admin-uphold-target@example.com");

        // Ban first, then the member appeals and wins.
        (await admin.Client.PostAsJsonAsync(
            $"/api/v1/admin/profiles/{target.ProfileId}/action",
            new { action = "ban", reason = "Initial decision." },
            Ct)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await ScalarAsync<string>("SELECT status FROM profile.profiles WHERE id = @id", target.ProfileId))
            .Should().Be("banned");

        var response = await admin.Client.PostAsJsonAsync(
            $"/api/v1/admin/appeals/{Guid.NewGuid()}/resolve",
            new { targetProfileId = target.ProfileId, outcome = "upheld", note = "Verified the profile is genuine." },
            Ct);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        doc.RootElement.GetProperty("restrictionLifted").GetBoolean().Should().BeTrue();

        (await ScalarAsync<string>("SELECT status FROM profile.profiles WHERE id = @id", target.ProfileId))
            .Should().Be("active", "an upheld appeal reinstates the member");
        (await CountAsync(
            "SELECT count(*) FROM moderation.moderation_actions WHERE target_user_id = @id AND action = 'clear'",
            ("id", target.UserId)))
            .Should().Be(1);
    }

    [Fact]
    public async Task Admin_warning_clears_the_profile_from_the_review_queue()
    {
        var admin = await CreateActiveParticipantAsync("admin-warn@example.com");
        await PromoteToAdminAsync(admin.UserId);
        var reporter = await CreateActiveParticipantAsync("admin-warn-reporter@example.com");
        var target = await CreateActiveParticipantAsync("admin-warn-target@example.com");

        (await ReportAsync(reporter.Client, target.ProfileId, "spam")).StatusCode.Should().Be(HttpStatusCode.Accepted);

        using (var queueDoc = await GetJsonAsync(admin.Client, "/api/v1/admin/queue"))
        {
            queueDoc.RootElement.EnumerateArray()
                .Any(i => i.GetProperty("profileId").GetGuid() == target.ProfileId)
                .Should().BeTrue();
        }

        (await admin.Client.PostAsJsonAsync(
            $"/api/v1/admin/profiles/{target.ProfileId}/action",
            new { action = "warn", reason = "First and final warning." },
            Ct)).StatusCode.Should().Be(HttpStatusCode.OK);

        // A warning resolves the reports, so the profile leaves the queue, and it does
        // not change feed eligibility (still active).
        using (var queueDoc = await GetJsonAsync(admin.Client, "/api/v1/admin/queue"))
        {
            queueDoc.RootElement.EnumerateArray()
                .Any(i => i.GetProperty("profileId").GetGuid() == target.ProfileId)
                .Should().BeFalse();
        }

        (await ScalarAsync<string>("SELECT status FROM profile.profiles WHERE id = @id", target.ProfileId))
            .Should().Be("active");
    }

    [Fact]
    public async Task Admin_reports_filter_with_an_unknown_status_returns_nothing()
    {
        var admin = await CreateActiveParticipantAsync("admin-filter@example.com");
        await PromoteToAdminAsync(admin.UserId);
        var reporter = await CreateActiveParticipantAsync("admin-filter-reporter@example.com");
        var target = await CreateActiveParticipantAsync("admin-filter-target@example.com");
        (await ReportAsync(reporter.Client, target.ProfileId, "spam")).StatusCode.Should().Be(HttpStatusCode.Accepted);

        // 'banned' is a profile status, not a report status — it must not leak every report.
        using var doc = await GetJsonAsync(admin.Client, "/api/v1/admin/reports?status=banned");
        doc.RootElement.GetArrayLength().Should().Be(0);
    }

    private Task<HttpResponseMessage> ReportAsync(HttpClient client, Guid targetProfileId, string type) =>
        client.PostAsJsonAsync("/api/v1/reports", new { targetProfileId, type, note = (string?)null }, Ct);

    private async Task<JsonDocument> GetJsonAsync(HttpClient client, string uri)
    {
        var response = await client.GetAsync(uri, Ct);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
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
            countryCode = "RO",
            bio = "Here for appreciation, not scores.",
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

    private Task PromoteToAdminAsync(Guid userId) =>
        ExecuteAsync(
            "UPDATE identity.users SET role = 'admin' WHERE id = @id",
            p => p.AddWithValue("id", userId));

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
