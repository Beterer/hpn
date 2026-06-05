namespace Hpn.Modules.Photo.Internal.ImageProcessing;

internal sealed class PhotoUploadOptions
{
    public const long DefaultMaxUploadBytes = 8 * 1024 * 1024;
    public long MaxUploadBytes { get; init; } = DefaultMaxUploadBytes;
    public int DisplayMaxEdge { get; init; } = 1400;
    public int ThumbMaxEdge { get; init; } = 360;
    public int WebpQuality { get; init; } = 82;
    public int MaxPhotosPerProfile { get; init; } = 10;
}
