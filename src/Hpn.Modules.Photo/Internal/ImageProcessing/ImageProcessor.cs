using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Hpn.Modules.Photo.Internal.ImageProcessing;

internal sealed class ImageProcessor(IOptions<PhotoUploadOptions> options)
{
    private readonly PhotoUploadOptions _options = options.Value;

    public async Task<ProcessedPhoto> ProcessAsync(IFormFile file, CancellationToken cancellationToken)
    {
        await using var input = file.OpenReadStream();
        using var image = await Image.LoadAsync(input, cancellationToken);

        image.Mutate(context => context.AutoOrient());
        StripMetadata(image);

        var original = await EncodeWebpAsync(image, cancellationToken);
        var display = await EncodeVariantAsync(image, _options.DisplayMaxEdge, cancellationToken);
        var thumb = await EncodeVariantAsync(image, _options.ThumbMaxEdge, cancellationToken);

        return new ProcessedPhoto(
            image.Width,
            image.Height,
            Convert.ToHexString(SHA256.HashData(original)).ToLowerInvariant(),
            original,
            display,
            thumb);
    }

    private async Task<byte[]> EncodeVariantAsync(Image source, int maxEdge, CancellationToken cancellationToken)
    {
        using var variant = source.Clone(context => context.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(maxEdge, maxEdge),
        }));

        StripMetadata(variant);
        return await EncodeWebpAsync(variant, cancellationToken);
    }

    private async Task<byte[]> EncodeWebpAsync(Image image, CancellationToken cancellationToken)
    {
        await using var output = new MemoryStream();
        await image.SaveAsWebpAsync(output, new WebpEncoder { Quality = _options.WebpQuality }, cancellationToken);
        return output.ToArray();
    }

    private static void StripMetadata(Image image)
    {
        image.Metadata.ExifProfile = null;
        image.Metadata.IccProfile = null;
        image.Metadata.IptcProfile = null;
        image.Metadata.XmpProfile = null;
    }
}
