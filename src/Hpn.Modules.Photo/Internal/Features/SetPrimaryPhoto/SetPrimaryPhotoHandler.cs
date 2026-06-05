using Hpn.Modules.Photo.Internal.Features;
using Hpn.Modules.Photo.Internal.Persistence;
using Hpn.Modules.Profile.Contracts;
using Hpn.SharedKernel.Auth;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Photo.Internal.Features.SetPrimaryPhoto;

internal sealed record SetPrimaryPhotoResult(
    IReadOnlyCollection<PhotoResponse> Photos,
    bool ProfileMissing,
    bool PhotoMissing);

internal sealed class SetPrimaryPhotoHandler(
    PhotoDbContext dbContext,
    ICurrentUser currentUser,
    IProfileApi profileApi)
{
    public async Task<SetPrimaryPhotoResult> HandleAsync(Guid photoId, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        var profileId = await profileApi.GetProfileIdForUserAsync(userId, cancellationToken);
        if (profileId is null)
        {
            return new SetPrimaryPhotoResult([], ProfileMissing: true, PhotoMissing: false);
        }

        var photos = await dbContext.Photos
            .Where(p => p.ProfileId == profileId.Value)
            .OrderBy(p => p.Position)
            .ToArrayAsync(cancellationToken);
        var selected = photos.FirstOrDefault(p => p.Id == photoId);
        if (selected is null)
        {
            return new SetPrimaryPhotoResult([], ProfileMissing: false, PhotoMissing: true);
        }

        if (!selected.IsPrimary)
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var current = photos.FirstOrDefault(p => p.IsPrimary);
            if (current is not null)
            {
                current.SetPrimary(false);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            selected.SetPrimary(true);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        return new SetPrimaryPhotoResult(
            photos.Select(PhotoResponses.ToResponse).ToArray(),
            ProfileMissing: false,
            PhotoMissing: false);
    }
}
