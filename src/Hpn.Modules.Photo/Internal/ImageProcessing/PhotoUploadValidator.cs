using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Hpn.Modules.Photo.Internal.ImageProcessing;

internal sealed class PhotoUploadValidator(IOptions<PhotoUploadOptions> options)
{
    private static readonly byte[] PngMagic = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private readonly PhotoUploadOptions _options = options.Value;

    public async Task<PhotoUploadValidationResult> ValidateAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return PhotoUploadValidationResult.Invalid("Choose a photo file to upload.");
        }

        if (file.Length > _options.MaxUploadBytes)
        {
            return PhotoUploadValidationResult.Invalid(
                $"Photo must be no larger than {_options.MaxUploadBytes / 1024 / 1024} MB.");
        }

        if (!IsAllowedMime(file.ContentType))
        {
            return PhotoUploadValidationResult.Invalid("Photo must be a JPEG, PNG, or WebP image.");
        }

        await using var stream = file.OpenReadStream();
        var header = new byte[12];
        var bytesRead = await stream.ReadAsync(header, cancellationToken);
        if (!MagicMatchesContentType(file.ContentType, header.AsSpan(0, bytesRead)))
        {
            return PhotoUploadValidationResult.Invalid("The uploaded file does not match its image type.");
        }

        return PhotoUploadValidationResult.Valid;
    }

    private static bool IsAllowedMime(string? contentType) =>
        contentType is "image/jpeg" or "image/png" or "image/webp";

    private static bool MagicMatchesContentType(string? contentType, ReadOnlySpan<byte> header) =>
        contentType switch
        {
            "image/jpeg" => header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            "image/png" => header.StartsWith(PngMagic),
            "image/webp" => IsWebp(header),
            _ => false,
        };

    private static bool IsWebp(ReadOnlySpan<byte> header) =>
        header.Length >= 12 &&
        header[0] == (byte)'R' &&
        header[1] == (byte)'I' &&
        header[2] == (byte)'F' &&
        header[3] == (byte)'F' &&
        header[8] == (byte)'W' &&
        header[9] == (byte)'E' &&
        header[10] == (byte)'B' &&
        header[11] == (byte)'P';
}
