using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Hpn.IntegrationTests.Identity;
using Hpn.Modules.Identity.Internal.Email;
using Hpn.Modules.Photo.Internal.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Xunit;

namespace Hpn.IntegrationTests.Development;

public sealed class DevelopmentSeedFlowTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();
    private readonly CapturingEmailSender _emails = new();
    private readonly RecordingObjectStore _objectStore = new();
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
    public async Task Development_seed_creates_a_working_test_account()
    {
        var client = await SignInAsync("test@notice.local");

        using (var profile = await GetJsonAsync(client, "/api/v1/profile/me"))
        {
            profile.RootElement.GetProperty("displayName").GetString().Should().Be("Test Notice");
            profile.RootElement.GetProperty("status").GetString().Should().Be("active");
            profile.RootElement.GetProperty("verified").GetBoolean().Should().BeTrue();
        }

        using var feed = await GetJsonAsync(client, "/api/v1/feed/next?limit=20");
        var cards = feed.RootElement.EnumerateArray().ToArray();
        cards.Should().NotBeEmpty();
        cards.Select(c => c.GetProperty("displayName").GetString()).Should().NotContain("Test Notice");
        cards.Should().OnlyContain(c => c.GetProperty("photos").GetArrayLength() > 0);

        var firstCard = cards[0];
        var firstPhoto = firstCard.GetProperty("photos").EnumerateArray().First();
        var photoResponse = await client.GetAsync(firstPhoto.GetProperty("displayUrl").GetString(), Ct);
        photoResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        photoResponse.Content.Headers.ContentType!.MediaType.Should().Be("image/webp");
        (await photoResponse.Content.ReadAsByteArrayAsync(Ct)).Should().NotBeEmpty();

        var categories = await GetCategoriesAsync(client);
        var categoryId = categories.RootElement.EnumerateArray().First().GetProperty("id").GetGuid();
        using var submit = new HttpRequestMessage(HttpMethod.Post, "/api/v1/appreciations")
        {
            Content = JsonContent.Create(new
            {
                receiverProfileId = firstCard.GetProperty("profileId").GetGuid(),
                categoryId,
                photoId = firstPhoto.GetProperty("photoId").GetGuid(),
            }),
        };
        submit.Headers.Add("Idempotency-Key", "development-seed-flow-submit");
        var submitted = await client.SendAsync(submit, Ct);
        submitted.StatusCode.Should().Be(HttpStatusCode.Created);

        using (var received = await GetJsonAsync(client, "/api/v1/appreciations/received?includeEvents=true"))
        {
            received.RootElement.GetProperty("total").GetInt32().Should().BeGreaterThanOrEqualTo(24);
            received.RootElement.GetProperty("events").GetArrayLength().Should().BeGreaterThan(0);
        }

        using (var fingerprint = await GetJsonAsync(client, "/api/v1/fingerprint/me"))
        {
            fingerprint.RootElement.GetProperty("status").GetString().Should().Be("ready");
            fingerprint.RootElement.GetProperty("sampleSize").GetInt32().Should().BeGreaterThanOrEqualTo(24);
        }

        using (var style = await GetJsonAsync(client, "/api/v1/appreciation-style/me"))
        {
            style.RootElement.GetProperty("status").GetString().Should().Be("ready");
            style.RootElement.GetProperty("total").GetInt32().Should().BeGreaterThan(0);
        }

        UseFactory();
        var rerunClient = await SignInAsync("test@notice.local");
        using var receivedAfterRerun = await GetJsonAsync(rerunClient, "/api/v1/appreciations/received");
        receivedAfterRerun.RootElement.GetProperty("total").GetInt32().Should().BeGreaterThanOrEqualTo(24);
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

    private async Task<JsonDocument> GetJsonAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url, Ct);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
    }

    private async Task<JsonDocument> GetCategoriesAsync(HttpClient client) =>
        await GetJsonAsync(client, "/api/v1/appreciation-categories");

    private void UseFactory()
    {
        _factory?.Dispose();
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:Postgres", _postgres.GetConnectionString());
            builder.UseSetting("DevelopmentSeed:Enabled", "true");
            builder.UseSetting("DevelopmentSeed:ImageDirectory", FindSeedImageDirectory());
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IEmailSender>();
                services.AddSingleton<IEmailSender>(_emails);
                services.RemoveAll<IObjectStore>();
                services.AddSingleton<IObjectStore>(_objectStore);
            });
        });
    }

    private static string FindSeedImageDirectory()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "seed", "images");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not find seed/images from the test working directory.");
    }

    private static string ExtractSessionCookie(HttpResponseMessage response)
    {
        var header = response.Headers.GetValues("Set-Cookie")
            .First(c => c.StartsWith("hpn_session=", StringComparison.Ordinal));
        return header.Split(';', 2)[0];
    }

    private sealed class RecordingObjectStore : IObjectStore
    {
        private readonly ConcurrentDictionary<string, ObjectVariant> _objects = new(StringComparer.Ordinal);

        public Task PutAsync(ObjectVariant variant, CancellationToken cancellationToken)
        {
            _objects[variant.Key] = variant;
            return Task.CompletedTask;
        }

        public Task<StoredObject?> GetAsync(string key, CancellationToken cancellationToken)
        {
            if (!_objects.TryGetValue(key, out var variant))
            {
                return Task.FromResult<StoredObject?>(null);
            }

            return Task.FromResult<StoredObject?>(
                new StoredObject(new MemoryStream(variant.Bytes, writable: false), variant.ContentType));
        }

        public Task DeleteAsync(string key, CancellationToken cancellationToken)
        {
            _objects.TryRemove(key, out _);
            return Task.CompletedTask;
        }
    }
}
