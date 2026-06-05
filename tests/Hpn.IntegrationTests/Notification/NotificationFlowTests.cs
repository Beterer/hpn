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

namespace Hpn.IntegrationTests.Notification;

/// <summary>
/// Notification flow over real Postgres: create, summary, privacy, and mark-seen.
/// </summary>
public sealed class NotificationFlowTests : IAsyncLifetime
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
    public async Task Receiving_an_appreciation_creates_an_unseen_notification_surfaced_by_summary()
    {
        var target = await CreateActiveParticipantAsync("notif-target@example.com");
        var sender = await CreateActiveParticipantAsync("notif-sender@example.com");
        var trait = await GetTraitAsync(sender.Client, "warm_smile");

        var before = await GetSummaryAsync(target.Client);
        before.GetProperty("unseenCount").GetInt32().Should().Be(0);
        before.GetProperty("latest").ValueKind.Should().Be(JsonValueKind.Null);

        (await SubmitAsync(sender.Client, target.ProfileId, trait.Id, target.PhotoId, "notif-warm"))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        var after = await GetSummaryAsync(target.Client);
        after.GetProperty("unseenCount").GetInt32().Should().Be(1);
        var latest = after.GetProperty("latest");
        latest.GetProperty("type").GetString().Should().Be("appreciation_received");
        latest.GetProperty("traitLabel").GetString().Should().Be("Warm smile");
        latest.GetProperty("categorySlug").GetString().Should().Be("physical");
        latest.GetProperty("seen").GetBoolean().Should().BeFalse();
        latest.TryGetProperty("senderUserId", out _).Should().BeFalse();

        (await GetSummaryAsync(sender.Client)).GetProperty("unseenCount").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task Marking_seen_clears_the_dot()
    {
        var target = await CreateActiveParticipantAsync("notif-seen-target@example.com");
        var sender = await CreateActiveParticipantAsync("notif-seen-sender@example.com");
        var trait = await GetTraitAsync(sender.Client, "warm_smile");

        (await SubmitAsync(sender.Client, target.ProfileId, trait.Id, target.PhotoId, "notif-seen"))
            .StatusCode.Should().Be(HttpStatusCode.Created);
        (await GetSummaryAsync(target.Client)).GetProperty("unseenCount").GetInt32().Should().Be(1);

        var seen = await target.Client.PostAsync("/api/v1/notifications/seen", content: null, Ct);
        seen.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var after = await GetSummaryAsync(target.Client);
        after.GetProperty("unseenCount").GetInt32().Should().Be(0);
        after.GetProperty("latest").GetProperty("seen").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Summary_requires_authentication()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/notifications/summary", Ct);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }

    private sealed record Trait(Guid Id, Guid CategoryId, string Slug, string Label, int SortOrder);

    private sealed record Category(Guid Id, string Slug, string Label, int SortOrder, int Hue, Trait[] Traits);

    private async Task<JsonElement> GetSummaryAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/notifications/summary", Ct);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        return doc.RootElement.Clone();
    }

    private async Task<Trait> GetTraitAsync(HttpClient client, string slug)
    {
        var response = await client.GetAsync("/api/v1/appreciation-categories", Ct);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var categories = await response.Content.ReadFromJsonAsync<Category[]>(cancellationToken: Ct);
        return categories!.SelectMany(c => c.Traits).Single(t => t.Slug == slug);
    }

    private async Task<HttpResponseMessage> SubmitAsync(
        HttpClient client,
        Guid receiverProfileId,
        Guid traitId,
        Guid? photoId,
        string idempotencyKey)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/appreciations")
        {
            Content = JsonContent.Create(new { receiverProfileId, traitId, photoId }),
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return await client.SendAsync(request, Ct);
    }

    private sealed record Participant(HttpClient Client, Guid ProfileId, Guid PhotoId);

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
        var photoId = await InsertReadyPhotoAsync(profileId);
        await ExecuteAsync(
            "UPDATE profile.profiles SET status = 'active', updated_at = now() WHERE id = @id",
            p => p.AddWithValue("id", profileId));
        return new Participant(client, profileId, photoId);
    }

    private async Task<HttpClient> SignInAsync(string email)
    {
        var client = _factory.CreateClient();
        (await client.PostAsJsonAsync("/api/v1/auth/magic-link", new { email }, Ct))
            .StatusCode.Should().Be(HttpStatusCode.Accepted);
        var token = _emails.LastTokenFor(email);
        token.Should().NotBeNullOrEmpty();
        var verified = await client.PostAsJsonAsync("/api/v1/auth/verify", new { token }, Ct);
        verified.StatusCode.Should().Be(HttpStatusCode.OK);
        client.DefaultRequestHeaders.Add("Cookie", ExtractSessionCookie(verified));
        return client;
    }

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

    private static string ExtractSessionCookie(HttpResponseMessage response) =>
        response.Headers.GetValues("Set-Cookie")
            .First(c => c.StartsWith("hpn_session=", StringComparison.Ordinal))
            .Split(';', 2)[0];
}
