using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Hpn.IntegrationTests.Identity;
using Hpn.Modules.Feed.Internal.Ranking;
using Hpn.Modules.Identity.Internal.Email;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Hpn.IntegrationTests.Feed;

/// <summary>
/// M4 feed over real Postgres: the eligibility read model (backbone §6.5) and the
/// pluggable ranking strategy. Eligibility inputs that have no API until later
/// milestones (blocks → M8, verification → admin, appreciations → M5) are seeded
/// directly into their owning module's tables — exactly the cross-schema state the
/// read model is responsible for honouring.
/// </summary>
public sealed class FeedFlowTests : IAsyncLifetime
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
    public async Task Returns_eligible_others_and_excludes_self_inactive_and_photoless()
    {
        var viewer = await CreateActiveParticipantAsync("viewer@example.com");
        var eligible = await CreateActiveParticipantAsync("eligible@example.com");
        var draft = await CreateDraftParticipantAsync("draft@example.com");
        var photoless = await CreateActiveNoPhotoParticipantAsync("photoless@example.com");

        var feed = await GetFeedAsync(viewer.Client);

        feed.Should().Contain(eligible.ProfileId);
        feed.Should().NotContain(viewer.ProfileId);   // self
        feed.Should().NotContain(draft.ProfileId);     // not active
        feed.Should().NotContain(photoless.ProfileId); // no ready photo
    }

    [Fact]
    public async Task Feed_card_carries_visibility_checked_photo_urls()
    {
        var viewer = await CreateActiveParticipantAsync("shape-viewer@example.com");
        var target = await CreateActiveParticipantAsync("shape-target@example.com");

        using var doc = await GetFeedDocumentAsync(viewer.Client);
        var card = doc.RootElement
            .EnumerateArray()
            .Single(e => e.GetProperty("profileId").GetGuid() == target.ProfileId);

        card.GetProperty("displayName").GetString().Should().Be("shape-target");
        card.TryGetProperty("status", out _).Should().BeFalse("the feed never exposes status (§2)");

        var photo = card.GetProperty("photos").EnumerateArray().Should().ContainSingle().Subject;
        photo.GetProperty("photoId").GetGuid().Should().NotBeEmpty();
        photo.GetProperty("position").GetInt32().Should().Be(0);
        photo.GetProperty("displayUrl").GetString().Should().Be(
            $"/api/v1/photos/{photo.GetProperty("photoId").GetGuid()}/content?variant=display");
        photo.GetProperty("thumbUrl").GetString().Should().EndWith("variant=thumb");
    }

    [Fact]
    public async Task Excludes_already_appreciated_profiles()
    {
        var viewer = await CreateActiveParticipantAsync("appr-viewer@example.com");
        var appreciated = await CreateActiveParticipantAsync("appreciated@example.com");
        var fresh = await CreateActiveParticipantAsync("fresh@example.com");

        await InsertAppreciationAsync(viewer.UserId, appreciated.ProfileId);

        var feed = await GetFeedAsync(viewer.Client);

        feed.Should().Contain(fresh.ProfileId);
        feed.Should().NotContain(appreciated.ProfileId);
    }

    [Fact]
    public async Task Excludes_blocked_profiles_in_both_directions()
    {
        var viewer = await CreateActiveParticipantAsync("block-viewer@example.com");
        var blockedByViewer = await CreateActiveParticipantAsync("blocked-by-viewer@example.com");
        var blocksViewer = await CreateActiveParticipantAsync("blocks-viewer@example.com");
        var unrelated = await CreateActiveParticipantAsync("unrelated@example.com");

        await InsertBlockAsync(viewer.UserId, blockedByViewer.UserId);
        await InsertBlockAsync(blocksViewer.UserId, viewer.UserId);

        var feed = await GetFeedAsync(viewer.Client);

        feed.Should().Contain(unrelated.ProfileId);
        feed.Should().NotContain(blockedByViewer.ProfileId);
        feed.Should().NotContain(blocksViewer.ProfileId);
    }

    [Fact]
    public async Task Honours_women_for_women_in_both_directions()
    {
        var manViewer = await CreateActiveParticipantAsync("man-viewer@example.com", gender: "man");
        var womanOnlyTarget = await CreateActiveParticipantAsync("w4w-target@example.com", gender: "woman");
        var openWoman = await CreateActiveParticipantAsync("open-woman@example.com", gender: "woman");
        await SetVisibilityFlagAsync(womanOnlyTarget.ProfileId, "women_for_women", true);

        var manFeed = await GetFeedAsync(manViewer.Client);
        manFeed.Should().Contain(openWoman.ProfileId);
        manFeed.Should().NotContain(womanOnlyTarget.ProfileId, "a women-for-women profile is hidden from a man");

        // A viewer in women-for-women mode sees only women.
        var womanViewer = await CreateActiveParticipantAsync("woman-viewer@example.com", gender: "woman");
        await SetVisibilityFlagAsync(womanViewer.ProfileId, "women_for_women", true);
        var manTarget = await CreateActiveParticipantAsync("man-target@example.com", gender: "man");

        var womanFeed = await GetFeedAsync(womanViewer.Client);
        womanFeed.Should().Contain(openWoman.ProfileId);
        womanFeed.Should().Contain(womanOnlyTarget.ProfileId);
        womanFeed.Should().NotContain(manTarget.ProfileId);
    }

    [Fact]
    public async Task Honours_verified_only_in_both_directions()
    {
        var viewer = await CreateActiveParticipantAsync("verif-viewer@example.com");
        var verifiedTarget = await CreateActiveParticipantAsync("verified-target@example.com");
        var unverifiedTarget = await CreateActiveParticipantAsync("unverified-target@example.com");
        await SetVerifiedAsync(verifiedTarget.ProfileId, true);

        // Viewer-side: I only want to see verified people.
        await SetVisibilityFlagAsync(viewer.ProfileId, "verified_only", true);
        var viewerFeed = await GetFeedAsync(viewer.Client);
        viewerFeed.Should().Contain(verifiedTarget.ProfileId);
        viewerFeed.Should().NotContain(unverifiedTarget.ProfileId);

        // Candidate-side: a verified-only profile is hidden from an unverified viewer.
        var unverifiedViewer = await CreateActiveParticipantAsync("unverified-viewer@example.com");
        var verifiedOnlyTarget = await CreateActiveParticipantAsync("verified-only-target@example.com");
        await SetVisibilityFlagAsync(verifiedOnlyTarget.ProfileId, "verified_only", true);

        var unverifiedFeed = await GetFeedAsync(unverifiedViewer.Client);
        unverifiedFeed.Should().NotContain(verifiedOnlyTarget.ProfileId);
    }

    [Fact]
    public async Task Honours_country_visibility_rules()
    {
        var viewer = await CreateActiveParticipantAsync("country-viewer@example.com", country: "RO");
        var sameCountryHidden = await CreateActiveParticipantAsync("hide-from-country@example.com", country: "RO");
        var sameCountryOpen = await CreateActiveParticipantAsync("same-country-open@example.com", country: "RO");
        await SetVisibilityFlagAsync(sameCountryHidden.ProfileId, "hide_from_country", true);

        var feed = await GetFeedAsync(viewer.Client);
        feed.Should().Contain(sameCountryOpen.ProfileId);
        feed.Should().NotContain(sameCountryHidden.ProfileId, "this profile hides from same-country viewers");

        // Viewer-side: I only want to see people outside my country.
        var outsideViewer = await CreateActiveParticipantAsync("outside-viewer@example.com", country: "RO");
        await SetVisibilityFlagAsync(outsideViewer.ProfileId, "show_only_outside_country", true);
        var abroad = await CreateActiveParticipantAsync("abroad@example.com", country: "US");

        var outsideFeed = await GetFeedAsync(outsideViewer.Client);
        outsideFeed.Should().Contain(abroad.ProfileId);
        outsideFeed.Should().NotContain(sameCountryOpen.ProfileId);
    }

    [Fact]
    public async Task Applies_session_level_seen_dedupe()
    {
        var viewer = await CreateActiveParticipantAsync("seen-viewer@example.com");
        var target = await CreateActiveParticipantAsync("seen-target@example.com");
        var other = await CreateActiveParticipantAsync("seen-other@example.com");

        var unfiltered = await GetFeedAsync(viewer.Client);
        unfiltered.Should().Contain(target.ProfileId);

        var deduped = await GetFeedAsync(viewer.Client, seen: [target.ProfileId]);
        deduped.Should().Contain(other.ProfileId);
        deduped.Should().NotContain(target.ProfileId);
    }

    [Fact]
    public async Task Ranking_strategy_is_swappable_without_touching_eligibility()
    {
        // Re-host with a deterministic strategy registered in place of the random
        // one — the ONLY change needed to alter feed ordering (backbone §6.5).
        UseFactory(services =>
        {
            services.RemoveAll<IFeedRankingStrategy>();
            services.AddScoped<IFeedRankingStrategy, AscendingIdStrategy>();
        });

        var viewer = await CreateActiveParticipantAsync("rank-viewer@example.com");
        var a = await CreateActiveParticipantAsync("rank-a@example.com");
        var b = await CreateActiveParticipantAsync("rank-b@example.com");
        var c = await CreateActiveParticipantAsync("rank-c@example.com");

        var feed = await GetFeedAsync(viewer.Client);

        var expected = new[] { a.ProfileId, b.ProfileId, c.ProfileId }.OrderBy(id => id).ToArray();
        feed.Should().Equal(expected, "the swapped strategy alone dictates ordering");
    }

    /// <summary>A deterministic stand-in for the random strategy — sorts ascending.</summary>
    private sealed class AscendingIdStrategy : IFeedRankingStrategy
    {
        public IReadOnlyList<Guid> Select(IReadOnlyList<Guid> eligibleProfileIds, FeedViewerContext viewer, int batchSize) =>
            eligibleProfileIds.OrderBy(id => id).Take(batchSize).ToArray();
    }

    // ---- feed helpers ------------------------------------------------------

    private async Task<IReadOnlyList<Guid>> GetFeedAsync(
        HttpClient client,
        IReadOnlyCollection<Guid>? seen = null)
    {
        using var doc = await GetFeedDocumentAsync(client, seen);
        return doc.RootElement.EnumerateArray().Select(e => e.GetProperty("profileId").GetGuid()).ToArray();
    }

    private async Task<JsonDocument> GetFeedDocumentAsync(
        HttpClient client,
        IReadOnlyCollection<Guid>? seen = null)
    {
        var url = "/api/v1/feed/next?limit=20";
        if (seen is { Count: > 0 })
        {
            url += "&seen=" + string.Join(',', seen);
        }

        var response = await client.GetAsync(url, Ct);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
    }

    // ---- participant setup -------------------------------------------------

    private sealed record Participant(HttpClient Client, Guid ProfileId, Guid UserId);

    private async Task<Participant> CreateActiveParticipantAsync(
        string email,
        string gender = "woman",
        string country = "RO")
    {
        var participant = await CreateDraftParticipantAsync(email, gender, country);
        await InsertReadyPhotoAsync(participant.ProfileId);
        await SetStatusAsync(participant.ProfileId, "active");
        return participant;
    }

    private async Task<Participant> CreateActiveNoPhotoParticipantAsync(string email)
    {
        var participant = await CreateDraftParticipantAsync(email);
        await SetStatusAsync(participant.ProfileId, "active");
        return participant;
    }

    private async Task<Participant> CreateDraftParticipantAsync(
        string email,
        string gender = "woman",
        string country = "RO")
    {
        var client = await SignInAsync(email);
        var created = await client.PutAsJsonAsync("/api/v1/profile", new
        {
            displayName = email.Split('@')[0],
            gender,
            selfDescribeText = (string?)null,
            countryCode = country,
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

    // ---- raw cross-schema seeding (no API until later milestones) -----------

    private Task SetStatusAsync(Guid profileId, string status) => ExecuteAsync(
        "UPDATE profile.profiles SET status = @status, updated_at = now() WHERE id = @id",
        p =>
        {
            p.AddWithValue("status", status);
            p.AddWithValue("id", profileId);
        });

    private Task SetVerifiedAsync(Guid profileId, bool verified) => ExecuteAsync(
        "UPDATE profile.profiles SET verified = @verified, updated_at = now() WHERE id = @id",
        p =>
        {
            p.AddWithValue("verified", verified);
            p.AddWithValue("id", profileId);
        });

    private Task SetVisibilityFlagAsync(Guid profileId, string column, bool value)
    {
        // column is from a fixed allow-list below, never user input.
        var allowed = new[]
        {
            "women_for_women", "verified_only", "hide_from_country", "show_only_outside_country", "paused",
        };
        column.Should().BeOneOf(allowed);
        return ExecuteAsync(
            $"UPDATE profile.visibility_preferences SET {column} = @value WHERE profile_id = @id",
            p =>
            {
                p.AddWithValue("value", value);
                p.AddWithValue("id", profileId);
            });
    }

    private Task InsertBlockAsync(Guid blockerUserId, Guid blockedUserId) => ExecuteAsync(
        "INSERT INTO profile.user_blocks (blocker_user_id, blocked_user_id, created_at) " +
        "VALUES (@blocker, @blocked, now())",
        p =>
        {
            p.AddWithValue("blocker", blockerUserId);
            p.AddWithValue("blocked", blockedUserId);
        });

    private Task InsertAppreciationAsync(Guid senderUserId, Guid receiverProfileId) => ExecuteAsync(
        "INSERT INTO appreciation.appreciation_events " +
        "(id, sender_user_id, receiver_profile_id, category_id, idempotency_key, created_at) " +
        "VALUES (@id, @sender, @receiver, (SELECT id FROM appreciation.appreciation_categories ORDER BY sort_order LIMIT 1), @key, now())",
        p =>
        {
            var eventId = Guid.NewGuid();
            p.AddWithValue("id", eventId);
            p.AddWithValue("sender", senderUserId);
            p.AddWithValue("receiver", receiverProfileId);
            p.AddWithValue("key", $"feed-test-{eventId}");
        });

    private Task InsertReadyPhotoAsync(Guid profileId)
    {
        var photoId = Guid.NewGuid();
        var prefix = $"profiles/{profileId}/photos/{photoId}";
        var contentHash = (photoId.ToString("N") + photoId.ToString("N"))[..64];
        return ExecuteAsync(
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

    private async Task<Guid> ScalarGuidAsync(string sql, Action<NpgsqlParameterCollection> bind)
    {
        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync(Ct);
        await using var command = new NpgsqlCommand(sql, connection);
        bind(command.Parameters);
        var result = await command.ExecuteScalarAsync(Ct);
        return (Guid)result!;
    }

    private void UseFactory(Action<IServiceCollection>? configureServices = null)
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
                configureServices?.Invoke(services);
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
