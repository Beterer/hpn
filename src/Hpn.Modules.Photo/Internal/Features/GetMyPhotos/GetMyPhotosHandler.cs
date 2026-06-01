using Hpn.Modules.Photo.Internal.Persistence;
using Hpn.Modules.Profile.Contracts;
using Hpn.SharedKernel.Auth;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Photo.Internal.Features.GetMyPhotos;

internal sealed record GetMyPhotosResult(IReadOnlyCollection<PhotoResponse> Photos, bool ProfileMissing);

internal sealed class GetMyPhotosHandler(
    PhotoDbContext dbContext,
    ICurrentUser currentUser,
    IProfileApi profileApi)
{
    public async Task<GetMyPhotosResult> HandleAsync(CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        var profileId = await profileApi.GetProfileIdForUserAsync(userId, cancellationToken);
        if (profileId is null)
        {
            return new GetMyPhotosResult([], ProfileMissing: true);
        }

        var photos = await dbContext.Photos
            .AsNoTracking()
            .Where(p => p.ProfileId == profileId.Value)
            .OrderBy(p => p.Position)
            .Select(p => PhotoResponses.ToResponse(p))
            .ToArrayAsync(cancellationToken);

        return new GetMyPhotosResult(photos, ProfileMissing: false);
    }
}
