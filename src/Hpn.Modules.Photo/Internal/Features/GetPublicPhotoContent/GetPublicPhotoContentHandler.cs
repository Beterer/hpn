using Hpn.Modules.Photo.Internal.Domain;
using Hpn.Modules.Photo.Internal.Persistence;
using Hpn.Modules.Photo.Internal.Storage;
using Hpn.Modules.Profile.Contracts;
using Hpn.SharedKernel.Auth;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Photo.Internal.Features.GetPublicPhotoContent;

internal sealed record GetPublicPhotoContentResult(
    Stream? Content,
    string? ContentType,
    bool NotFound,
    bool InvalidVariant);

/// <summary>
/// Serves a processed photo variant of <em>any</em> profile to a viewer, gated by
/// Profile's visibility contract (backbone §6.5, §11). This is the feed/card
/// serving path; it differs from the owner-only
/// <c>GET /profile/photos/{id}/content</c> in that the viewer is not the owner.
/// Only <c>display</c>/<c>thumb</c> are ever exposed — never the original.
/// </summary>
internal sealed class GetPublicPhotoContentHandler(
    PhotoDbContext dbContext,
    ICurrentUser currentUser,
    IProfileApi profileApi,
    IObjectStore objectStore)
{
    public async Task<GetPublicPhotoContentResult> HandleAsync(
        Guid photoId,
        string? variant,
        CancellationToken cancellationToken)
    {
        var viewerId = currentUser.RequireActorId();

        var photo = await dbContext.Photos
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == photoId, cancellationToken);
        if (photo is null)
        {
            return new GetPublicPhotoContentResult(null, null, NotFound: true, InvalidVariant: false);
        }

        // Only ready photos are ever served publicly. A processing/rejected (and,
        // in phase 2, NSFW-flagged) photo must never leak through a guessed id,
        // even for a visible profile (§6.3, §10.2).
        if (photo.Status != PhotoStatus.Ready)
        {
            return new GetPublicPhotoContentResult(null, null, NotFound: true, InvalidVariant: false);
        }

        // Visibility is Profile's call (active, not paused, no block in either
        // direction). A hidden photo 404s rather than 403s — we never confirm the
        // existence of a profile the viewer may not see.
        var visible = await profileApi.IsVisibleToAsync(
            photo.ProfileId,
            viewerId,
            enforceGuestRestrictions: currentUser.ActorKind == ActorKind.Guest,
            cancellationToken);
        if (!visible)
        {
            return new GetPublicPhotoContentResult(null, null, NotFound: true, InvalidVariant: false);
        }

        var key = variant switch
        {
            null or "" or "display" => photo.DisplayKey,
            "thumb" => photo.ThumbKey,
            _ => null,
        };

        if (key is null)
        {
            return new GetPublicPhotoContentResult(null, null, NotFound: false, InvalidVariant: true);
        }

        var stored = await objectStore.GetAsync(key, cancellationToken);
        return stored is null
            ? new GetPublicPhotoContentResult(null, null, NotFound: true, InvalidVariant: false)
            : new GetPublicPhotoContentResult(stored.Content, stored.ContentType, NotFound: false, InvalidVariant: false);
    }
}
