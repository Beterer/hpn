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

namespace Hpn.IntegrationTests.Appreciation;

/// <summary>
/// M5 over real Postgres: category seed data, idempotent appreciation submission,
/// atomic counters/style projection, and feed recency after the core loop.
/// </summary>
public sealed class AppreciationFlowTests : IAsyncLifetime
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
    public async Task Submit_persists_event_updates_counters_and_unlocks_next_profile()
    {
        var viewer = await CreateActiveParticipantAsync("appr-viewer@example.com");
        var target = await CreateActiveParticipantAsync("appr-target@example.com");
        var next = await CreateActiveParticipantAsync("appr-next@example.com");
        var category = (await GetCategoriesAsync(viewer.Client)).Single(c => c.Slug == "warm_smile");

        var initialFeed = await GetFeedAsync(viewer.Client);
        initialFeed.Should().Contain(target.ProfileId);

        var submitted = await SubmitAsync(
            viewer.Client,
            target.ProfileId,
            category.Id,
            target.PhotoId,
            "submit-updates-counters");

        submitted.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await ReadJsonAsync(submitted);
        body.GetProperty("receiverProfileId").GetGuid().Should().Be(target.ProfileId);
        body.GetProperty("categoryLabel").GetString().Should().Be("Warm smile");
        body.GetProperty("replayed").GetBoolean().Should().BeFalse();
        body.GetProperty("nextProfileUnlocked").GetBoolean().Should().BeTrue();

        var receivedCount = await ScalarIntAsync(
            "SELECT count FROM appreciation.received_appreciation_stats WHERE receiver_profile_id = @profile AND category_id = @category",
            p =>
            {
                p.AddWithValue("profile", target.ProfileId);
                p.AddWithValue("category", category.Id);
            });
        receivedCount.Should().Be(1);

        var givenCount = await ScalarIntAsync(
            "SELECT count FROM appreciation.given_appreciation_stats WHERE sender_user_id = @sender AND category_id = @category",
            p =>
            {
                p.AddWithValue("sender", viewer.UserId);
                p.AddWithValue("category", category.Id);
            });
        givenCount.Should().Be(1);

        var feedAfter = await GetFeedAsync(viewer.Client);
        feedAfter.Should().Contain(next.ProfileId);
        feedAfter.Should().NotContain(target.ProfileId, "already-appreciated profiles drop out of the feed");
    }

    [Fact]
    public async Task Duplicate_is_rejected_and_idempotency_replays_without_double_counting()
    {
        var viewer = await CreateActiveParticipantAsync("idem-viewer@example.com");
        var target = await CreateActiveParticipantAsync("idem-target@example.com");
        var categories = await GetCategoriesAsync(viewer.Client);
        var first = categories[0];
        var second = categories[1];

        var created = await SubmitAsync(viewer.Client, target.ProfileId, first.Id, target.PhotoId, "same-key");
        created.StatusCode.Should().Be(HttpStatusCode.Created);

        var replay = await SubmitAsync(viewer.Client, target.ProfileId, first.Id, target.PhotoId, "same-key");
        replay.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadJsonAsync(replay)).GetProperty("replayed").GetBoolean().Should().BeTrue();

        var duplicate = await SubmitAsync(viewer.Client, target.ProfileId, first.Id, target.PhotoId, "new-key");
        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var conflict = await SubmitAsync(viewer.Client, target.ProfileId, second.Id, target.PhotoId, "same-key");
        conflict.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var receivedCount = await ScalarIntAsync(
            "SELECT count FROM appreciation.received_appreciation_stats WHERE receiver_profile_id = @profile AND category_id = @category",
            p =>
            {
                p.AddWithValue("profile", target.ProfileId);
                p.AddWithValue("category", first.Id);
            });
        receivedCount.Should().Be(1);
    }

    [Fact]
    public async Task Submit_rejects_self_and_invisible_receivers()
    {
        var viewer = await CreateActiveParticipantAsync("reject-viewer@example.com");
        var draft = await CreateDraftParticipantAsync("reject-draft@example.com");
        var category = (await GetCategoriesAsync(viewer.Client))[0];

        var self = await SubmitAsync(viewer.Client, viewer.ProfileId, category.Id, viewer.PhotoId, "self-key");
        self.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var invisible = await SubmitAsync(viewer.Client, draft.ProfileId, category.Id, null, "invisible-key");
        invisible.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Received_view_returns_private_perception_summary_without_ranking_copy()
    {
        var target = await CreateActiveParticipantAsync("received-target@example.com");
        var senderOne = await CreateActiveParticipantAsync("received-sender-one@example.com");
        var senderTwo = await CreateActiveParticipantAsync("received-sender-two@example.com");
        var senderThree = await CreateActiveParticipantAsync("received-sender-three@example.com");
        var categories = await GetCategoriesAsync(target.Client);
        var warmSmile = categories.Single(c => c.Slug == "warm_smile");
        var creative = categories.Single(c => c.Slug == "creative");

        (await SubmitAsync(senderOne.Client, target.ProfileId, creative.Id, target.PhotoId, "received-creative-one"))
            .StatusCode.Should().Be(HttpStatusCode.Created);
        (await SubmitAsync(senderTwo.Client, target.ProfileId, creative.Id, target.PhotoId, "received-creative-two"))
            .StatusCode.Should().Be(HttpStatusCode.Created);
        (await SubmitAsync(senderThree.Client, target.ProfileId, warmSmile.Id, target.PhotoId, "received-warm"))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await target.Client.GetAsync("/api/v1/appreciations/received?includeEvents=true", Ct);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        var root = doc.RootElement;

        root.GetProperty("profileId").GetGuid().Should().Be(target.ProfileId);
        root.GetProperty("headline").GetString().Should().Be("People often describe you in these ways.");
        root.GetProperty("summary").GetString().Should().Contain("perceived");
        root.GetProperty("total").GetInt32().Should().Be(3);

        var receivedCategories = root.GetProperty("categories").EnumerateArray().ToArray();
        receivedCategories.Select(c => c.GetProperty("slug").GetString()).Should().Equal("warm_smile", "creative");
        receivedCategories[0].GetProperty("count").GetInt32().Should().Be(1);
        receivedCategories[1].GetProperty("count").GetInt32().Should().Be(2);
        receivedCategories[0].GetProperty("phrasing").GetString().Should().StartWith("People often");

        var events = root.GetProperty("events").EnumerateArray().ToArray();
        events.Should().HaveCount(3);
        events[0].TryGetProperty("senderUserId", out _).Should().BeFalse();
        events[0].GetProperty("phrasing").GetString().Should().StartWith("Someone");

        var body = root.GetRawText().ToLowerInvariant();
        body.Should().NotContain("score");
        body.Should().NotContain("rank");
        body.Should().NotContain("leaderboard");
        body.Should().NotContain("popular");

        var senderView = await senderOne.Client.GetAsync("/api/v1/appreciations/received", Ct);
        senderView.StatusCode.Should().Be(HttpStatusCode.OK);
        using var senderDoc = JsonDocument.Parse(await senderView.Content.ReadAsStringAsync(Ct));
        senderDoc.RootElement.GetProperty("profileId").GetGuid().Should().Be(senderOne.ProfileId);
        senderDoc.RootElement.GetProperty("total").GetInt32().Should().Be(0);
    }

    private sealed record Category(Guid Id, string Slug, string Label, int SortOrder);

    private async Task<Category[]> GetCategoriesAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/appreciation-categories", Ct);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var categories = await response.Content.ReadFromJsonAsync<Category[]>(cancellationToken: Ct);
        categories.Should().NotBeNull();
        categories.Should().HaveCount(12);
        categories!.Select(c => c.Slug).Should().Equal(
            "warm_smile",
            "authentic",
            "stylish",
            "calming_energy",
            "confident",
            "expressive",
            "fun_energy",
            "elegant",
            "trustworthy",
            "creative",
            "kind",
            "intelligent");
        return categories;
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.Clone();
    }

    private async Task<HttpResponseMessage> SubmitAsync(
        HttpClient client,
        Guid receiverProfileId,
        Guid categoryId,
        Guid? photoId,
        string idempotencyKey)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/appreciations")
        {
            Content = JsonContent.Create(new
            {
                receiverProfileId,
                categoryId,
                photoId,
            }),
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return await client.SendAsync(request, Ct);
    }

    private async Task<IReadOnlyList<Guid>> GetFeedAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/feed/next?limit=20", Ct);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        return doc.RootElement.EnumerateArray().Select(e => e.GetProperty("profileId").GetGuid()).ToArray();
    }

    private sealed record Participant(HttpClient Client, Guid ProfileId, Guid UserId, Guid PhotoId);

    private async Task<Participant> CreateActiveParticipantAsync(string email)
    {
        var participant = await CreateDraftParticipantAsync(email);
        var photoId = await InsertReadyPhotoAsync(participant.ProfileId);
        await SetStatusAsync(participant.ProfileId, "active");
        return participant with { PhotoId = photoId };
    }

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
        return new Participant(client, profileId, userId, PhotoId: Guid.Empty);
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

    private Task SetStatusAsync(Guid profileId, string status) => ExecuteAsync(
        "UPDATE profile.profiles SET status = @status, updated_at = now() WHERE id = @id",
        p =>
        {
            p.AddWithValue("status", status);
            p.AddWithValue("id", profileId);
        });

    private async Task<Guid> InsertReadyPhotoAsync(Guid profileId)
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
        return photoId;
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
            builder.UseSetting("DevelopmentSeed:Enabled", "false");
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
