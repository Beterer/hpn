using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using Hpn.IntegrationTests.Identity;
using Hpn.Modules.Identity.Internal.Email;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using Testcontainers.PostgreSql;
using Xunit;

namespace Hpn.IntegrationTests.Profile;

/// <summary>
/// M3 photo flow over real Postgres + MinIO: upload processing, storage, ordering,
/// deletion, and the min-one-ready-photo activation rule.
/// </summary>
public sealed class PhotoFlowTests : IAsyncLifetime
{
    private const string AccessKey = "hpn";
    private const string SecretKey = "hpn-secret";
    private const string BucketName = "hpn-photos-test";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();
    private readonly IContainer _minio = new ContainerBuilder("minio/minio:latest")
        .WithEnvironment("MINIO_ROOT_USER", AccessKey)
        .WithEnvironment("MINIO_ROOT_PASSWORD", SecretKey)
        .WithCommand("server", "/data")
        .WithPortBinding(9000, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(9000))
        .Build();

    private readonly CapturingEmailSender _emails = new();
    private WebApplicationFactory<Program> _factory = null!;

    public async ValueTask InitializeAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await _postgres.StartAsync(ct);
        await _minio.StartAsync(ct);

        using var s3 = CreateS3Client();
        await s3.PutBucketAsync(new PutBucketRequest { BucketName = BucketName }, ct);

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:Postgres", _postgres.GetConnectionString());
            builder.UseSetting("Photo:MaxUploadBytes", "65536");
            builder.UseSetting("Photo:Storage:BucketName", BucketName);
            builder.UseSetting("Photo:Storage:ServiceUrl", MinioUrl);
            builder.UseSetting("Photo:Storage:AccessKey", AccessKey);
            builder.UseSetting("Photo:Storage:SecretKey", SecretKey);
            builder.UseSetting("Photo:Storage:Region", "us-east-1");
            builder.UseSetting("Photo:Storage:ForcePathStyle", "true");
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
        await _minio.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Upload_processes_variants_and_allows_activation_then_order_and_delete()
    {
        var client = await SignInAsync("photo-owner@example.com");
        var ct = TestContext.Current.CancellationToken;

        var created = await CreateProfileAsync(client, "Rowan", ct);
        var profileId = await ReadGuidAsync(created, "id", ct);

        var blockedActivation = await client.PutAsJsonAsync("/api/v1/profile/status", new { status = "active" }, ct);
        blockedActivation.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await blockedActivation.Content.ReadAsStringAsync(ct)).Should().Contain("Photo required");

        using var first = await UploadPhotoAsync(client, await CreateJpegAsync(96, 72, includeExif: true, ct), "first.jpg", ct);
        first.RootElement.GetProperty("status").GetString().Should().Be("ready");
        first.RootElement.GetProperty("position").GetInt32().Should().Be(0);
        var firstPhotoId = first.RootElement.GetProperty("id").GetGuid();

        await AssertStoredWebpVariantsAsync(profileId, firstPhotoId, ct);

        var activated = await client.PutAsJsonAsync("/api/v1/profile/status", new { status = "active" }, ct);
        activated.StatusCode.Should().Be(HttpStatusCode.OK);

        using var second = await UploadPhotoAsync(client, await CreateJpegAsync(80, 80, includeExif: false, ct), "second.jpg", ct);
        var secondPhotoId = second.RootElement.GetProperty("id").GetGuid();

        var reordered = await client.PutAsJsonAsync("/api/v1/profile/photos/order", new
        {
            photoIds = new[] { secondPhotoId, firstPhotoId },
        }, ct);
        reordered.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var doc = await ReadJsonAsync(reordered, ct))
        {
            var photos = doc.RootElement.EnumerateArray().ToArray();
            photos[0].GetProperty("id").GetGuid().Should().Be(secondPhotoId);
            photos[0].GetProperty("position").GetInt32().Should().Be(0);
            photos[1].GetProperty("position").GetInt32().Should().Be(1);
        }

        var deleted = await client.DeleteAsync($"/api/v1/profile/photos/{secondPhotoId}", ct);
        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var remaining = await client.GetAsync("/api/v1/profile/photos", ct);
        remaining.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var doc = await ReadJsonAsync(remaining, ct))
        {
            var photo = doc.RootElement.EnumerateArray().Should().ContainSingle().Subject;
            photo.GetProperty("id").GetGuid().Should().Be(firstPhotoId);
            photo.GetProperty("position").GetInt32().Should().Be(0);
        }
    }

    [Fact]
    public async Task Upload_rejects_bad_type_and_oversize()
    {
        var client = await SignInAsync("bad-photo@example.com");
        var ct = TestContext.Current.CancellationToken;
        _ = await CreateProfileAsync(client, "Mira", ct);

        var badType = await UploadRawAsync(
            client,
            Encoding.UTF8.GetBytes("not an image"),
            "image/jpeg",
            "fake.jpg",
            ct);
        badType.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var oversize = await UploadRawAsync(
            client,
            new byte[65537],
            "image/jpeg",
            "too-big.jpg",
            ct);
        oversize.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Public_photo_serving_is_visibility_checked()
    {
        var ownerClient = await SignInAsync("public-photo-owner@example.com");
        var ct = TestContext.Current.CancellationToken;

        _ = await CreateProfileAsync(ownerClient, "Mira", ct);
        using var uploaded = await UploadPhotoAsync(
            ownerClient, await CreateJpegAsync(96, 72, includeExif: false, ct), "photo.jpg", ct);
        var photoId = uploaded.RootElement.GetProperty("id").GetGuid();

        var activated = await ownerClient.PutAsJsonAsync("/api/v1/profile/status", new { status = "active" }, ct);
        activated.StatusCode.Should().Be(HttpStatusCode.OK);

        var viewerClient = await SignInAsync("public-photo-viewer@example.com");

        // Visible profile → a viewer who is not the owner can read display + thumb.
        var display = await viewerClient.GetAsync($"/api/v1/photos/{photoId}/content?variant=display", ct);
        display.StatusCode.Should().Be(HttpStatusCode.OK);
        display.Content.Headers.ContentType!.MediaType.Should().Be("image/webp");

        var thumb = await viewerClient.GetAsync($"/api/v1/photos/{photoId}/content?variant=thumb", ct);
        thumb.StatusCode.Should().Be(HttpStatusCode.OK);

        // Unknown variant is a 400; the original is never an option.
        var badVariant = await viewerClient.GetAsync($"/api/v1/photos/{photoId}/content?variant=original", ct);
        badVariant.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var missing = await viewerClient.GetAsync($"/api/v1/photos/{Guid.NewGuid()}/content", ct);
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Pause the owner → the photo is no longer visible to the viewer (404, not 403).
        var paused = await ownerClient.PutAsJsonAsync("/api/v1/profile/status", new { status = "paused" }, ct);
        paused.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterPause = await viewerClient.GetAsync($"/api/v1/photos/{photoId}/content?variant=display", ct);
        afterPause.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // The owner can still read their own paused photo through the owner-scoped path.
        var ownerOwn = await ownerClient.GetAsync($"/api/v1/profile/photos/{photoId}/content?variant=display", ct);
        ownerOwn.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private string MinioUrl => $"http://{_minio.Hostname}:{_minio.GetMappedPublicPort(9000)}";

    private AmazonS3Client CreateS3Client() => new(
        AccessKey,
        SecretKey,
        new AmazonS3Config
        {
            ServiceURL = MinioUrl,
            ForcePathStyle = true,
            AuthenticationRegion = "us-east-1",
        });

    private async Task AssertStoredWebpVariantsAsync(Guid profileId, Guid photoId, CancellationToken cancellationToken)
    {
        using var s3 = CreateS3Client();
        foreach (var variant in new[] { "original", "display", "thumb" })
        {
            var key = $"profiles/{profileId}/photos/{photoId}/{variant}.webp";
            using var response = await s3.GetObjectAsync(BucketName, key, cancellationToken);
            response.Headers.ContentType.Should().Be("image/webp");
            await using var memory = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memory, cancellationToken);
            memory.Position = 0;
            using var image = await Image.LoadAsync(memory, cancellationToken);
            image.Metadata.ExifProfile.Should().BeNull();
            image.Metadata.IptcProfile.Should().BeNull();
            image.Metadata.XmpProfile.Should().BeNull();
        }
    }

    private async Task<JsonDocument> UploadPhotoAsync(
        HttpClient client,
        byte[] bytes,
        string fileName,
        CancellationToken cancellationToken)
    {
        var response = await UploadRawAsync(client, bytes, "image/jpeg", fileName, cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await ReadJsonAsync(response, cancellationToken);
    }

    private static async Task<HttpResponseMessage> UploadRawAsync(
        HttpClient client,
        byte[] bytes,
        string contentType,
        string fileName,
        CancellationToken cancellationToken)
    {
        using var form = new MultipartFormDataContent();
        using var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        form.Add(content, "file", fileName);
        return await client.PostAsync("/api/v1/profile/photos", form, cancellationToken);
    }

    private static async Task<HttpResponseMessage> CreateProfileAsync(
        HttpClient client,
        string displayName,
        CancellationToken cancellationToken)
    {
        var created = await client.PutAsJsonAsync("/api/v1/profile", new
        {
            displayName,
            gender = "woman",
            selfDescribeText = (string?)null,
            countryCode = "RO",
            bio = "Here for appreciation, not scores.",
        }, cancellationToken);
        created.StatusCode.Should().Be(HttpStatusCode.OK);
        return created;
    }

    private async Task<HttpClient> SignInAsync(string email)
    {
        var client = _factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        var requested = await client.PostAsJsonAsync("/api/v1/auth/magic-link", new { email }, ct);
        requested.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var token = _emails.LastTokenFor(email);
        token.Should().NotBeNullOrEmpty();

        var verified = await client.PostAsJsonAsync("/api/v1/auth/verify", new { token }, ct);
        verified.StatusCode.Should().Be(HttpStatusCode.OK);

        var cookie = ExtractSessionCookie(verified);
        cookie.Should().NotBeNull();
        client.DefaultRequestHeaders.Add("Cookie", cookie);
        return client;
    }

    private static async Task<byte[]> CreateJpegAsync(
        int width,
        int height,
        bool includeExif,
        CancellationToken cancellationToken)
    {
        using var image = new Image<Rgba32>(width, height, new Rgba32(38, 166, 154));
        if (includeExif)
        {
            image.Metadata.ExifProfile = new ExifProfile();
            image.Metadata.ExifProfile.SetValue(ExifTag.Software, "hpn-test");
        }

        await using var output = new MemoryStream();
        await image.SaveAsJpegAsync(output, new JpegEncoder { Quality = 90 }, cancellationToken);
        return output.ToArray();
    }

    private static async Task<Guid> ReadGuidAsync(
        HttpResponseMessage response,
        string propertyName,
        CancellationToken cancellationToken)
    {
        using var doc = await ReadJsonAsync(response, cancellationToken);
        return doc.RootElement.GetProperty(propertyName).GetGuid();
    }

    private static async Task<JsonDocument> ReadJsonAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

    private static string? ExtractSessionCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            return null;
        }

        var header = cookies.FirstOrDefault(c => c.StartsWith("hpn_session=", StringComparison.Ordinal));
        return header?.Split(';', 2)[0];
    }
}
