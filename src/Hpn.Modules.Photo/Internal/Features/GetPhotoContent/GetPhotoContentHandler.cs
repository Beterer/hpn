using Hpn.Modules.Photo.Internal.Persistence;
using Hpn.Modules.Photo.Internal.Storage;
using Hpn.Modules.Profile.Contracts;
using Hpn.SharedKernel.Auth;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Photo.Internal.Features.GetPhotoContent;

internal sealed record GetPhotoContentResult(
    Stream? Content,
    string? ContentType,
    bool NotFound,
    bool InvalidVariant);

internal sealed class GetPhotoContentHandler(
    PhotoDbContext dbContext,
    ICurrentUser currentUser,
    IProfileApi profileApi,
    IObjectStore objectStore)
{
    public async Task<GetPhotoContentResult> HandleAsync(
        Guid photoId,
        string? variant,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        var profileId = await profileApi.GetProfileIdForUserAsync(userId, cancellationToken);
        if (profileId is null)
        {
            return new GetPhotoContentResult(null, null, NotFound: true, InvalidVariant: false);
        }

        var photo = await dbContext.Photos
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == photoId && p.ProfileId == profileId.Value, cancellationToken);
        if (photo is null)
        {
            return new GetPhotoContentResult(null, null, NotFound: true, InvalidVariant: false);
        }

        var key = variant switch
        {
            null or "" or "display" => photo.DisplayKey,
            "thumb" => photo.ThumbKey,
            _ => null,
        };

        if (key is null)
        {
            return new GetPhotoContentResult(null, null, NotFound: false, InvalidVariant: true);
        }

        var stored = await objectStore.GetAsync(key, cancellationToken);
        return stored is null
            ? new GetPhotoContentResult(null, null, NotFound: true, InvalidVariant: false)
            : new GetPhotoContentResult(stored.Content, stored.ContentType, NotFound: false, InvalidVariant: false);
    }
}
