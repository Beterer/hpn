using Hpn.Modules.Photo.Internal.Domain;
using Hpn.SharedKernel;

namespace Hpn.Modules.Photo.Internal.Features;

internal sealed record PhotoResponse(
    Guid Id,
    Guid ProfileId,
    int Position,
    bool IsPrimary,
    string Status,
    int Width,
    int Height,
    string DisplayUrl,
    string ThumbUrl,
    DateTimeOffset CreatedAt);

internal static class PhotoResponses
{
    public static PhotoResponse ToResponse(ProfilePhoto photo) => new(
        photo.Id,
        photo.ProfileId,
        photo.Position,
        photo.IsPrimary,
        photo.Status.ToStorageValue(),
        photo.Width,
        photo.Height,
        $"{ApiRoutes.Prefix}/profile/photos/{photo.Id}/content?variant=display",
        $"{ApiRoutes.Prefix}/profile/photos/{photo.Id}/content?variant=thumb",
        photo.CreatedAt);
}
