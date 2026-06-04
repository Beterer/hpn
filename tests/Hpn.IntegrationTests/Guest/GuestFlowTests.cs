using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Hpn.IntegrationTests.Identity;
using Hpn.Modules.Identity.Internal.Email;
using Hpn.Modules.Identity.Internal.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Hpn.IntegrationTests.Guest;

public sealed class GuestFlowTests : IAsyncLifetime
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

    [Fact]
    public async Task Guest_can_browse_react_and_count_toward_recipient()
    {
        var target = await CreateActiveParticipantAsync("guest-target@example.com");
        var other = await CreateActiveParticipantAsync("guest-other@example.com");
        var guest = await StartGuestAsync();
        var trait = await FirstTraitAsync(guest.Client);

        var feed = await GetFeedAsync(guest.Client);
        feed.Should().Contain(new[] { target.ProfileId, other.ProfileId });

        var submitted = await SubmitAsync(guest.Client, target.ProfileId, trait.Id, "guest-reacts");
        submitted.StatusCode.Should().Be(HttpStatusCode.Created);

        var receivedCount = await ScalarIntAsync(
            "SELECT count FROM appreciation.received_appreciation_stats WHERE receiver_profile_id = @profile AND category_id = @category",
            p =>
            {
                p.AddWithValue("profile", target.ProfileId);
                p.AddWithValue("category", trait.CategoryId);
            });
        receivedCount.Should().Be(1);

        var givenCount = await ScalarIntAsync(
            "SELECT count FROM appreciation.given_appreciation_stats WHERE sender_user_id = @sender AND category_id = @category",
            p =>
            {
                p.AddWithValue("sender", guest.GuestId);
                p.AddWithValue("category", trait.CategoryId);
            });
        givenCount.Should().Be(1);

        (await GetFeedAsync(guest.Client)).Should().NotContain(target.ProfileId);
    }

    [Fact]
    public async Task Hidden_from_guests_excludes_guest_feed_only()
    {
        var memberViewer = await CreateActiveParticipantAsync("member-viewer@example.com");
        var hidden = await CreateActiveParticipantAsync("hidden-from-guests@example.com");
        await SetHiddenFromGuestsAsync(hidden.ProfileId, true);

        var guest = await StartGuestAsync();
        (await GetFeedAsync(guest.Client)).Should().NotContain(hidden.ProfileId);

        (await GetFeedAsync(memberViewer.Client)).Should().Contain(hidden.ProfileId);
    }

    [Fact]
    public async Task Conversion_rekeys_guest_appreciations_to_the_member()
    {
        var target = await CreateActiveParticipantAsync("convert-target@example.com");
        var guest = await StartGuestAsync();
        var trait = await FirstTraitAsync(guest.Client);
        (await SubmitAsync(guest.Client, target.ProfileId, trait.Id, "convert-guest")).StatusCode
            .Should().Be(HttpStatusCode.Created);

        var verified = await VerifyMagicLinkAsync(guest.Client, "converted@example.com");
        var userId = verified.UserId;

        (await ScalarIntAsync(
            "SELECT COUNT(*) FROM appreciation.appreciation_events WHERE sender_user_id = @id",
            p => p.AddWithValue("id", guest.GuestId))).Should().Be(0);
        (await ScalarIntAsync(
            "SELECT COUNT(*) FROM appreciation.appreciation_events WHERE sender_user_id = @id",
            p => p.AddWithValue("id", userId))).Should().Be(1);

        var givenCount = await ScalarIntAsync(
            "SELECT count FROM appreciation.given_appreciation_stats WHERE sender_user_id = @sender AND category_id = @category",
            p =>
            {
                p.AddWithValue("sender", userId);
                p.AddWithValue("category", trait.CategoryId);
            });
        givenCount.Should().Be(1);

        var guestSession = await QuerySingleAsync(
            "SELECT revoked_at IS NOT NULL, converted_to_user_id FROM identity.guest_sessions WHERE id = @id",
            p => p.AddWithValue("id", guest.GuestId),
            reader => (Revoked: reader.GetBoolean(0), ConvertedTo: reader.GetGuid(1)));
        guestSession.Revoked.Should().BeTrue();
        guestSession.ConvertedTo.Should().Be(userId);
    }

    [Fact]
    public async Task Conversion_collision_keeps_member_event_and_merges_given_stats()
    {
        var member = await CreateActiveParticipantAsync("collision-member@example.com");
        var target = await CreateActiveParticipantAsync("collision-target@example.com");
        var trait = await FirstTraitAsync(member.Client);
        (await SubmitAsync(member.Client, target.ProfileId, trait.Id, "member-existing")).StatusCode
            .Should().Be(HttpStatusCode.Created);

        var guest = await StartGuestAsync();
        (await SubmitAsync(guest.Client, target.ProfileId, trait.Id, "guest-colliding")).StatusCode
            .Should().Be(HttpStatusCode.Created);

        await VerifyMagicLinkAsync(guest.Client, "collision-member@example.com");

        var eventCount = await ScalarIntAsync(
            """
            SELECT COUNT(*)
            FROM appreciation.appreciation_events
            WHERE sender_user_id = @sender AND receiver_profile_id = @receiver AND category_id = @category
            """,
            p =>
            {
                p.AddWithValue("sender", member.UserId);
                p.AddWithValue("receiver", target.ProfileId);
                p.AddWithValue("category", trait.CategoryId);
            });
        eventCount.Should().Be(1);

        var givenCount = await ScalarIntAsync(
            "SELECT count FROM appreciation.given_appreciation_stats WHERE sender_user_id = @sender AND category_id = @category",
            p =>
            {
                p.AddWithValue("sender", member.UserId);
                p.AddWithValue("category", trait.CategoryId);
            });
        givenCount.Should().Be(2);

        var receivedCount = await ScalarIntAsync(
            "SELECT count FROM appreciation.received_appreciation_stats WHERE receiver_profile_id = @receiver AND category_id = @category",
            p =>
            {
                p.AddWithValue("receiver", target.ProfileId);
                p.AddWithValue("category", trait.CategoryId);
            });
        receivedCount.Should().Be(1);
    }

    [Fact]
    public async Task Member_only_endpoints_reject_guest_cookie()
    {
        var guest = await StartGuestAsync();

        var me = await guest.Client.GetAsync("/api/v1/me", Ct);
        me.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);

        var profile = await guest.Client.PutAsJsonAsync("/api/v1/profile", new
        {
            displayName = "Guest",
            gender = "woman",
            selfDescribeText = (string?)null,
            countryCode = "RO",
            bio = "Trying to sneak through.",
        }, Ct);
        profile.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }

    private sealed record GuestClient(HttpClient Client, Guid GuestId);
    private sealed record Participant(HttpClient Client, Guid ProfileId, Guid UserId);
    private sealed record Trait(Guid Id, Guid CategoryId, string Slug, string Label, int SortOrder);
    private sealed record Category(Guid Id, string Slug, string Label, int SortOrder, int Hue, Trait[] Traits);
    private sealed record VerifiedUser(Guid UserId, string Cookie);

    private async Task<GuestClient> StartGuestAsync()
    {
        var client = _factory.CreateClient();
        var started = await client.PostAsync("/api/v1/guest/start", content: null, Ct);
        started.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var doc = JsonDocument.Parse(await started.Content.ReadAsStringAsync(Ct)))
        {
            doc.RootElement.GetProperty("nudgeThreshold").GetInt32().Should().BeGreaterThan(0);
        }

        var cookie = ExtractCookie(started, "hpn_guest");
        client.DefaultRequestHeaders.Add("Cookie", cookie);
        var rawToken = cookie.Split('=', 2)[1];
        var guestId = await ScalarGuidAsync(
            "SELECT id FROM identity.guest_sessions WHERE token_hash = @hash",
            p => p.AddWithValue("hash", TokenHasher.Hash(rawToken)));
        return new GuestClient(client, guestId);
    }

    private async Task<VerifiedUser> VerifyMagicLinkAsync(HttpClient client, string email)
    {
        var requested = await client.PostAsJsonAsync("/api/v1/auth/magic-link", new { email }, Ct);
        requested.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var token = _emails.LastTokenFor(email);
        token.Should().NotBeNullOrEmpty();

        var verified = await client.PostAsJsonAsync("/api/v1/auth/verify", new { token }, Ct);
        verified.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await verified.Content.ReadAsStringAsync(Ct));
        var userId = doc.RootElement.GetProperty("id").GetGuid();
        return new VerifiedUser(userId, ExtractCookie(verified, "hpn_session"));
    }

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
        await InsertReadyPhotoAsync(profileId);
        await SetStatusAsync(profileId, "active");
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
        client.DefaultRequestHeaders.Add("Cookie", ExtractCookie(verified, "hpn_session"));
        return client;
    }

    private async Task<Trait> FirstTraitAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/appreciation-categories", Ct);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var categories = await response.Content.ReadFromJsonAsync<Category[]>(cancellationToken: Ct);
        categories.Should().NotBeNull();
        return categories![0].Traits[0];
    }

    private async Task<HttpResponseMessage> SubmitAsync(
        HttpClient client,
        Guid receiverProfileId,
        Guid traitId,
        string idempotencyKey)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/appreciations")
        {
            Content = JsonContent.Create(new
            {
                receiverProfileId,
                traitId,
                photoId = (Guid?)null,
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

    private Task SetHiddenFromGuestsAsync(Guid profileId, bool hidden) => ExecuteAsync(
        "UPDATE profile.visibility_preferences SET hidden_from_guests = @hidden WHERE profile_id = @id",
        p =>
        {
            p.AddWithValue("hidden", hidden);
            p.AddWithValue("id", profileId);
        });

    private Task SetStatusAsync(Guid profileId, string status) => ExecuteAsync(
        "UPDATE profile.profiles SET status = @status, updated_at = now() WHERE id = @id",
        p =>
        {
            p.AddWithValue("status", status);
            p.AddWithValue("id", profileId);
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

    private async Task<int> ScalarIntAsync(string sql, Action<NpgsqlParameterCollection> bind)
    {
        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync(Ct);
        await using var command = new NpgsqlCommand(sql, connection);
        bind(command.Parameters);
        var result = await command.ExecuteScalarAsync(Ct);
        return Convert.ToInt32(result);
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

    private async Task<T> QuerySingleAsync<T>(string sql, Action<NpgsqlParameterCollection> bind, Func<NpgsqlDataReader, T> map)
    {
        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync(Ct);
        await using var command = new NpgsqlCommand(sql, connection);
        bind(command.Parameters);
        await using var reader = await command.ExecuteReaderAsync(Ct);
        (await reader.ReadAsync(Ct)).Should().BeTrue();
        return map(reader);
    }

    private static string ExtractCookie(HttpResponseMessage response, string name)
    {
        var header = response.Headers.GetValues("Set-Cookie")
            .First(c => c.StartsWith($"{name}=", StringComparison.Ordinal));
        return header.Split(';', 2)[0];
    }
}
