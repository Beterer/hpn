namespace Hpn.Modules.Photo.Internal.ImageProcessing;

internal sealed record ProcessedPhoto(
    int Width,
    int Height,
    string ContentHash,
    byte[] Original,
    byte[] Display,
    byte[] Thumb);
