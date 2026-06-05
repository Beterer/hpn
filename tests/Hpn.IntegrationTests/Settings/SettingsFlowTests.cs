using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Hpn.IntegrationTests.Identity;
using Hpn.Modules.Identity.Internal.Accounts;
using Hpn.Modules.Identity.Internal.Email;
using Hpn.Modules.Photo.Internal.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Hpn.IntegrationTests.Settings;

/// <summary>
/// M8 privacy &amp; settings over real Postgres (backbone §8 Settings, §10.4, §10.5):
/// the visibility/distance toggles changing eligibility, blocks via the API, and
/// two-phase account deletion (soft-delete + grace-window hard purge) including
/// object storage. The object store is faked with a recorder so the purge's blob
/// deletes are observable without standing up MinIO.
/// </summary>
public sealed class SettingsFlowTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();
    private readonly CapturingEmailSender _emails = new();
    private readonly RecordingObjectStore _objectStore = new();
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
                services.RemoveAll<IObjectStore>();
                services.AddSingleton<IObjectStore>(_objectStore);
            });
        });
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    // ---- visibility / distance --------------------------------------------

    [Fact]
    public async Task Minimum_distance_toggle_changes_feed_eligibility()
    {
        // Viewer in Bucharest who only wants people at least 100 km away.
        var viewer = await CreateActiveParticipantAsync("dist-viewer@example.com");
        await SetGeoAsync(viewer.ProfileId, 44.4, 26.1);

        var near = await CreateActiveParticipantAsync("near@example.com");      // ~Bucharest
        await SetGeoAsync(near.ProfileId, 44.45, 26.15);
        var far = await CreateActiveParticipantAsync("far@example.com");        // ~Cluj, ~250 km
        await SetGeoAsync(far.ProfileId, 46.77, 23.59);
        var noGeo = await CreateActiveParticipantAsync("nogeo@example.com");    // never shared a point

        // Before the toggle, everyone eligible shows.
        (await GetFeedAsync(viewer.Client)).Should().Contain(new[] { near.ProfileId, far.ProfileId, noGeo.ProfileId });

        var visibility = await viewer.Client.PutAsJsonAsync("/api/v1/settings/visibility", new
        {
            minDistanceKm = 100,
            womenForWomen = false,
            verifiedOnly = false,
            paused = false,
        }, Ct);
        visibility.StatusCode.Should().Be(HttpStatusCode.OK);

        var feed = await GetFeedAsync(viewer.Client);
        feed.Should().Contain(far.ProfileId, "they are beyond the 100 km minimum");
        feed.Should().NotContain(near.ProfileId, "they are inside the 100 km minimum");
        feed.Should().NotContain(noGeo.ProfileId, "an unmeasurable profile is excluded while the filter is on");
    }

    [Fact]
    public async Task Feed_card_carries_a_coarse_distance_bucket_only()
    {
        var viewer = await CreateActiveParticipantAsync("bucket-viewer@example.com");
        await SetGeoAsync(viewer.ProfileId, 44.4, 26.1);

        var near = await CreateActiveParticipantAsync("bucket-near@example.com");
        await SetGeoAsync(near.ProfileId, 44.42, 26.12); // a few km away

        var response = await viewer.Client.GetAsync("/api/v1/feed/next?limit=20", Ct);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        var card = doc.RootElement.EnumerateArray()
            .Single(e => e.GetProperty("profileId").GetGuid() == near.ProfileId);

        card.GetProperty("distanceBucket").GetString().Should().Be("nearby");
        // Never an exact coordinate or number on the wire (§10.4).
        card.TryGetProperty("geoLat", out _).Should().BeFalse();
        card.TryGetProperty("distanceKm", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Pause_toggle_hides_the_profile_from_the_feed()
    {
        var viewer = await CreateActiveParticipantAsync("pause-viewer@example.com");
        var target = await CreateActiveParticipantAsync("pause-target@example.com");

        (await GetFeedAsync(viewer.Client)).Should().Contain(target.ProfileId);

        var paused = await target.Client.PutAsJsonAsync("/api/v1/settings/visibility", new
        {
            minDistanceKm = (int?)null,
            womenForWomen = false,
            verifiedOnly = false,
            paused = true,
        }, Ct);
        paused.StatusCode.Should().Be(HttpStatusCode.OK);

        (await GetFeedAsync(viewer.Client)).Should().NotContain(target.ProfileId);
    }

    [Fact]
    public async Task Pause_and_unpause_via_settings_stay_in_sync_with_lifecycle_status()
    {
        var observer = await CreateActiveParticipantAsync("sync-observer@example.com");
        var target = await CreateActiveParticipantAsync("sync-target@example.com");

        await SetVisibilityPausedAsync(target.Client, true);
        (await StatusAsync(target.Client)).Should().Be("paused", "the toggle drives the lifecycle too");
        (await GetFeedAsync(observer.Client)).Should().NotContain(target.ProfileId);

        await SetVisibilityPausedAsync(target.Client, false);
        (await StatusAsync(target.Client)).Should().Be("active", "un-pausing re-activates");
        (await GetFeedAsync(observer.Client)).Should().Contain(target.ProfileId);
    }

    [Fact]
    public async Task Location_can_be_withdrawn_clearing_the_stored_point()
    {
        var participant = await CreateActiveParticipantAsync("loc@example.com");

        var set = await participant.Client.PutAsJsonAsync("/api/v1/settings/location", new
        {
            consent = true,
            latitude = 44.43,
            longitude = 26.12,
        }, Ct);
        set.StatusCode.Should().Be(HttpStatusCode.OK);

        // Coarse rounding to 0.1° is applied before storage (§10.4).
        var (lat, lng) = await GetGeoAsync(participant.ProfileId);
        lat.Should().Be(44.4);
        lng.Should().Be(26.1);

        var cleared = await participant.Client.PutAsJsonAsync("/api/v1/settings/location", new
        {
            consent = false,
            latitude = (double?)null,
            longitude = (double?)null,
        }, Ct);
        cleared.StatusCode.Should().Be(HttpStatusCode.OK);

        var (clearedLat, clearedLng) = await GetGeoAsync(participant.ProfileId);
        clearedLat.Should().BeNull();
        clearedLng.Should().BeNull();
    }

    // ---- blocks ------------------------------------------------------------

    [Fact]
    public async Task Block_via_api_removes_from_feed_both_ways_and_unblock_restores()
    {
        var viewer = await CreateActiveParticipantAsync("blk-viewer@example.com");
        var target = await CreateActiveParticipantAsync("blk-target@example.com");

        (await GetFeedAsync(viewer.Client)).Should().Contain(target.ProfileId);

        var blocked = await viewer.Client.PostAsJsonAsync(
            "/api/v1/settings/blocks", new { targetProfileId = target.ProfileId }, Ct);
        blocked.StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await GetFeedAsync(viewer.Client)).Should().NotContain(target.ProfileId);
        // and the block is honoured the other way, too.
        (await GetFeedAsync(target.Client)).Should().NotContain(viewer.ProfileId);

        var listed = await viewer.Client.GetFromJsonAsync<JsonElement>("/api/v1/settings/blocks", Ct);
        listed.EnumerateArray().Select(e => e.GetProperty("profileId").GetGuid())
            .Should().ContainSingle().Which.Should().Be(target.ProfileId);

        var unblocked = await viewer.Client.DeleteAsync($"/api/v1/settings/blocks/{target.ProfileId}", Ct);
        unblocked.StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await GetFeedAsync(viewer.Client)).Should().Contain(target.ProfileId);
    }

    [Fact]
    public async Task Cannot_block_yourself()
    {
        var viewer = await CreateActiveParticipantAsync("self-block@example.com");

        var response = await viewer.Client.PostAsJsonAsync(
            "/api/v1/settings/blocks", new { targetProfileId = viewer.ProfileId }, Ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---- account deletion + purge -----------------------------------------

    [Fact]
    public async Task Account_deletion_soft_deletes_then_purges_every_module_and_object_storage()
    {
        var observer = await CreateActiveParticipantAsync("obs@example.com");
        var victim = await CreateActiveParticipantAsync("victim@example.com");
        var blockee = await CreateActiveParticipantAsync("blockee@example.com");

        // Spread the victim's data across modules so the purge has something to remove.
        // (CreateActiveParticipantAsync already gave them one ready photo.)
        var photoKeys = victim.PhotoKeys;
        await InsertAppreciationAsync(victim.UserId, observer.ProfileId);       // victim as sender
        // victim as receiver — sent by the third party, so it doesn't mark the victim
        // already-appreciated for the observer (which would hide them from the feed).
        await InsertAppreciationAsync(blockee.UserId, victim.ProfileId);
        await InsertGivenStatAsync(victim.UserId);
        await InsertReceivedStatAsync(victim.ProfileId);
        await InsertSnapshotAsync(victim.ProfileId);
        // A block by the victim on a third party — purged with the account, and it
        // doesn't hide the victim from the observer (who has no block relationship).
        await InsertBlockAsync(victim.UserId, blockee.UserId);

        (await GetFeedAsync(observer.Client)).Should().Contain(victim.ProfileId);

        // Soft-delete: account hidden + session revoked immediately.
        var deleted = await victim.Client.PostAsync("/api/v1/settings/account/delete", content: null, Ct);
        deleted.StatusCode.Should().Be(HttpStatusCode.Accepted);

        (await GetFeedAsync(observer.Client)).Should().NotContain(victim.ProfileId, "the account left the feed at once");

        var afterDelete = await victim.Client.GetAsync("/api/v1/me", Ct);
        afterDelete.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "the session was revoked");

        // Hard purge: the gated maintenance step, run with the grace window elapsed.
        using (var scope = _factory.Services.CreateScope())
        {
            var purge = scope.ServiceProvider.GetRequiredService<AccountPurgeService>();
            var count = await purge.PurgeDueAsync(DateTimeOffset.UtcNow.AddDays(60), Ct);
            count.Should().Be(1);
        }

        (await CountAsync("SELECT count(*) FROM profile.profiles WHERE id = @id", victim.ProfileId))
            .Should().Be(0);
        (await CountAsync("SELECT count(*) FROM photo.photos WHERE profile_id = @id", victim.ProfileId))
            .Should().Be(0);
        (await CountAsync(
            "SELECT count(*) FROM appreciation.appreciation_events WHERE sender_user_id = @id OR receiver_profile_id = @id",
            victim.ProfileId, victim.UserId)).Should().Be(0);
        (await CountAsync("SELECT count(*) FROM appreciation.given_appreciation_stats WHERE sender_user_id = @id", victim.UserId))
            .Should().Be(0);
        (await CountAsync("SELECT count(*) FROM appreciation.received_appreciation_stats WHERE receiver_profile_id = @id", victim.ProfileId))
            .Should().Be(0);
        (await CountAsync("SELECT count(*) FROM social_fingerprint.social_fingerprint_snapshots WHERE profile_id = @id", victim.ProfileId))
            .Should().Be(0);
        (await CountAsync("SELECT count(*) FROM profile.user_blocks WHERE blocker_user_id = @id OR blocked_user_id = @id", victim.UserId, victim.UserId))
            .Should().Be(0);
        (await CountAsync("SELECT count(*) FROM identity.users WHERE id = @id", victim.UserId))
            .Should().Be(0);

        _objectStore.Deleted.Should().Contain(photoKeys, "the purge drops the binaries too (§10.5)");
    }

    // ---- export ------------------------------------------------------------

    [Fact]
    public async Task Account_export_includes_every_module_slice()
    {
        var participant = await CreateActiveParticipantAsync("export@example.com");
        // The participant already has one ready photo from setup.
        await InsertGivenStatAsync(participant.UserId);

        var export = await participant.Client.GetAsync("/api/v1/settings/account/export", Ct);
        export.StatusCode.Should().Be(HttpStatusCode.OK);
        export.Content.Headers.ContentDisposition!.FileName.Should().Contain("notice-account-export");

        using var doc = JsonDocument.Parse(await export.Content.ReadAsStringAsync(Ct));
        var root = doc.RootElement;

        root.GetProperty("account").GetProperty("email").GetString().Should().Be("export@example.com");
        root.GetProperty("profile").GetProperty("displayName").GetString().Should().Be("export");
        root.GetProperty("photos").ValueKind.Should().Be(JsonValueKind.Array);
        root.GetProperty("photos").GetArrayLength().Should().Be(1);
        root.GetProperty("appreciations").GetProperty("given").GetArrayLength().Should().Be(1);
    }

    private async Task SetVisibilityPausedAsync(HttpClient client, bool paused)
    {
        var response = await client.PutAsJsonAsync("/api/v1/settings/visibility", new
        {
            minDistanceKm = (int?)null,
            womenForWomen = false,
            verifiedOnly = false,
            paused,
        }, Ct);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<string?> StatusAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/profile/me", Ct);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        return doc.RootElement.GetProperty("status").GetString();
    }

    // ---- feed helpers ------------------------------------------------------

    private async Task<IReadOnlyList<Guid>> GetFeedAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/feed/next?limit=20", Ct);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        return doc.RootElement.EnumerateArray().Select(e => e.GetProperty("profileId").GetGuid()).ToArray();
    }

    // ---- participant setup -------------------------------------------------

    private sealed record Participant(HttpClient Client, Guid ProfileId, Guid UserId, string[] PhotoKeys);

    private async Task<Participant> CreateActiveParticipantAsync(
        string email,
        string gender = "woman")
    {
        var client = await SignInAsync(email);
        var created = await client.PutAsJsonAsync("/api/v1/profile", new
        {
            displayName = email.Split('@')[0],
            gender,
            selfDescribeText = (string?)null,
        }, Ct);
        created.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await created.Content.ReadAsStringAsync(Ct));
        var profileId = doc.RootElement.GetProperty("id").GetGuid();
        var userId = await ScalarAsync<Guid>("SELECT user_id FROM profile.profiles WHERE id = @id", profileId);

        var photoKeys = await InsertReadyPhotoAsync(profileId);
        await ExecuteAsync(
            "UPDATE profile.profiles SET status = 'active', updated_at = now() WHERE id = @id",
            p => p.AddWithValue("id", profileId));

        return new Participant(client, profileId, userId, photoKeys);
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

    // ---- raw seeding -------------------------------------------------------

    private Task SetGeoAsync(Guid profileId, double lat, double lng) => ExecuteAsync(
        "UPDATE profile.profiles SET geo_lat = @lat, geo_lng = @lng, location_consent = true WHERE id = @id",
        p =>
        {
            p.AddWithValue("lat", lat);
            p.AddWithValue("lng", lng);
            p.AddWithValue("id", profileId);
        });

    private async Task<(double? Lat, double? Lng)> GetGeoAsync(Guid profileId)
    {
        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync(Ct);
        await using var command = new NpgsqlCommand(
            "SELECT geo_lat, geo_lng FROM profile.profiles WHERE id = @id", connection);
        command.Parameters.AddWithValue("id", profileId);
        await using var reader = await command.ExecuteReaderAsync(Ct);
        await reader.ReadAsync(Ct);
        var lat = reader.IsDBNull(0) ? (double?)null : reader.GetDouble(0);
        var lng = reader.IsDBNull(1) ? (double?)null : reader.GetDouble(1);
        return (lat, lng);
    }

    private async Task<string[]> InsertReadyPhotoAsync(Guid profileId)
    {
        var photoId = Guid.NewGuid();
        var prefix = $"profiles/{profileId}/photos/{photoId}";
        var keys = new[] { $"{prefix}/original.webp", $"{prefix}/display.webp", $"{prefix}/thumb.webp" };
        var contentHash = (photoId.ToString("N") + photoId.ToString("N"))[..64];
        await ExecuteAsync(
            "INSERT INTO photo.photos " +
            "(id, profile_id, position, is_primary, status, original_key, display_key, thumb_key, width, height, content_hash, created_at) " +
            "VALUES (@id, @pid, 0, true, 'ready', @ok, @dk, @tk, 400, 400, @hash, now())",
            p =>
            {
                p.AddWithValue("id", photoId);
                p.AddWithValue("pid", profileId);
                p.AddWithValue("ok", keys[0]);
                p.AddWithValue("dk", keys[1]);
                p.AddWithValue("tk", keys[2]);
                p.AddWithValue("hash", contentHash);
            });
        return keys;
    }

    private Task InsertAppreciationAsync(Guid senderUserId, Guid receiverProfileId) => ExecuteAsync(
        "INSERT INTO appreciation.appreciation_events " +
        "(id, sender_user_id, receiver_profile_id, category_id, trait_id, idempotency_key, created_at) " +
        "SELECT @id, @sender, @receiver, t.category_id, t.id, @key, now() " +
        "FROM appreciation.appreciation_traits t ORDER BY t.sort_order LIMIT 1",
        p =>
        {
            var eventId = Guid.NewGuid();
            p.AddWithValue("id", eventId);
            p.AddWithValue("sender", senderUserId);
            p.AddWithValue("receiver", receiverProfileId);
            p.AddWithValue("key", $"settings-test-{eventId}");
        });

    private Task InsertGivenStatAsync(Guid senderUserId) => ExecuteAsync(
        "INSERT INTO appreciation.given_appreciation_stats (sender_user_id, category_id, count) " +
        "VALUES (@sender, (SELECT id FROM appreciation.appreciation_categories ORDER BY sort_order LIMIT 1), 1)",
        p => p.AddWithValue("sender", senderUserId));

    private Task InsertReceivedStatAsync(Guid receiverProfileId) => ExecuteAsync(
        "INSERT INTO appreciation.received_appreciation_stats (receiver_profile_id, category_id, count, last_at) " +
        "VALUES (@receiver, (SELECT id FROM appreciation.appreciation_categories ORDER BY sort_order LIMIT 1), 1, now())",
        p => p.AddWithValue("receiver", receiverProfileId));

    private Task InsertSnapshotAsync(Guid profileId) => ExecuteAsync(
        "INSERT INTO social_fingerprint.social_fingerprint_snapshots " +
        "(id, profile_id, period, period_start, sample_size, distribution, top_traits, created_at) " +
        "VALUES (@id, @pid, 'weekly', current_date, 20, '[]'::jsonb, '[]'::jsonb, now())",
        p =>
        {
            p.AddWithValue("id", Guid.NewGuid());
            p.AddWithValue("pid", profileId);
        });

    private Task InsertBlockAsync(Guid blockerUserId, Guid blockedUserId) => ExecuteAsync(
        "INSERT INTO profile.user_blocks (blocker_user_id, blocked_user_id, created_at) VALUES (@b, @t, now())",
        p =>
        {
            p.AddWithValue("b", blockerUserId);
            p.AddWithValue("t", blockedUserId);
        });

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

    private async Task<long> CountAsync(string sql, params Guid[] ids)
    {
        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync(Ct);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", ids[0]);
        return (long)(await command.ExecuteScalarAsync(Ct))!;
    }

    /// <summary>Stands in for S3/MinIO so blob deletes during purge are observable.</summary>
    private sealed class RecordingObjectStore : IObjectStore
    {
        public ConcurrentBag<string> Deleted { get; } = [];

        public Task PutAsync(ObjectVariant variant, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<StoredObject?> GetAsync(string key, CancellationToken cancellationToken) =>
            Task.FromResult<StoredObject?>(null);

        public Task DeleteAsync(string key, CancellationToken cancellationToken)
        {
            Deleted.Add(key);
            return Task.CompletedTask;
        }
    }
}
