using Hpn.Modules.Photo.Contracts.Events;
using Hpn.Modules.Photo.Internal.Persistence;
using Hpn.Modules.Photo.Internal.Storage;
using Hpn.Modules.Profile.Contracts;
using Hpn.SharedKernel.Auth;
using Hpn.SharedKernel.Events;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Photo.Internal.Features.DeleteProfilePhoto;

internal sealed record DeleteProfilePhotoResult(bool Removed, bool ProfileMissing);

internal sealed class DeleteProfilePhotoHandler(
    PhotoDbContext dbContext,
    ICurrentUser currentUser,
    IProfileApi profileApi,
    IObjectStore objectStore,
    TimeProvider timeProvider,
    IDomainEventDispatcher eventDispatcher)
{
    public async Task<DeleteProfilePhotoResult> HandleAsync(Guid photoId, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        var profileId = await profileApi.GetProfileIdForUserAsync(userId, cancellationToken);
        if (profileId is null)
        {
            return new DeleteProfilePhotoResult(Removed: false, ProfileMissing: true);
        }

        var photo = await dbContext.Photos
            .FirstOrDefaultAsync(p => p.Id == photoId && p.ProfileId == profileId.Value, cancellationToken);
        if (photo is null)
        {
            return new DeleteProfilePhotoResult(Removed: false, ProfileMissing: false);
        }

        var removedKeys = new[] { photo.OriginalKey, photo.DisplayKey, photo.ThumbKey };

        await using (var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken))
        {
            dbContext.Photos.Remove(photo);
            await dbContext.SaveChangesAsync(cancellationToken);

            var remaining = await dbContext.Photos
                .Where(p => p.ProfileId == profileId.Value)
                .OrderBy(p => p.Position)
                .ToArrayAsync(cancellationToken);

            if (photo.IsPrimary && remaining.Length > 0)
            {
                remaining[0].SetPrimary(true);
            }

            // Two-phase shift through a disjoint range so the (profile_id, position)
            // unique index can't trip mid-batch as rows slide down to close the gap.
            for (var index = 0; index < remaining.Length; index++)
            {
                remaining[index].MoveTo(remaining.Length + index);
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            for (var index = 0; index < remaining.Length; index++)
            {
                remaining[index].MoveTo(index);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        // The row is durably gone; only now drop the blobs. Best-effort, so a storage
        // hiccup leaves orphaned objects rather than a row pointing at missing content.
        foreach (var key in removedKeys)
        {
            try
            {
                await objectStore.DeleteAsync(key, cancellationToken);
            }
            catch (Exception)
            {
                // Swallow — the delete already succeeded; orphan blobs are the safe failure.
            }
        }

        await eventDispatcher.DispatchAsync(
            new PhotoRemoved(photo.Id, photo.ProfileId, timeProvider.GetUtcNow()),
            cancellationToken);

        return new DeleteProfilePhotoResult(Removed: true, ProfileMissing: false);
    }
}
