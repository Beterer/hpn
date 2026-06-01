using FluentAssertions;
using Hpn.Modules.Photo.Internal.ImageProcessing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Xunit;

namespace Hpn.Modules.Photo.Tests;

public sealed class PhotoUploadValidatorTests
{
    private readonly PhotoUploadValidator _validator = new(Options.Create(new PhotoUploadOptions
    {
        MaxUploadBytes = 16,
    }));

    [Fact]
    public async Task Validate_accepts_matching_jpeg_magic_bytes()
    {
        var file = CreateFile([0xFF, 0xD8, 0xFF, 0xAA], "image/jpeg");

        var result = await _validator.ValidateAsync(file, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_rejects_mime_magic_mismatch()
    {
        var file = CreateFile([0x00, 0x01, 0x02, 0x03], "image/jpeg");

        var result = await _validator.ValidateAsync(file, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.Problem.Should().Contain("does not match");
    }

    [Fact]
    public async Task Validate_rejects_oversize_upload()
    {
        var file = CreateFile(new byte[17], "image/jpeg");

        var result = await _validator.ValidateAsync(file, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.Problem.Should().Contain("no larger");
    }

    private static IFormFile CreateFile(byte[] bytes, string contentType)
    {
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", "photo.jpg")
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType,
        };
    }
}
