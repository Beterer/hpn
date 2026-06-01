namespace Hpn.Modules.Photo.Internal.Storage;

internal sealed record PhotoObjectKeys(string OriginalKey, string DisplayKey, string ThumbKey)
{
    public static PhotoObjectKeys ForPhoto(Guid profileId, Guid photoId)
    {
        var prefix = $"profiles/{profileId}/photos/{photoId}";
        return new PhotoObjectKeys(
            $"{prefix}/original.webp",
            $"{prefix}/display.webp",
            $"{prefix}/thumb.webp");
    }
}
