using Hpn.Modules.Profile.Internal.Features;
using Hpn.Modules.Profile.Internal.Persistence;
using Hpn.SharedKernel.Auth;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Profile.Internal.Features.GetPublicProfile;

internal sealed class GetPublicProfileHandler(ProfileDbContext dbContext, ICurrentUser currentUser)
{
    public async Task<PublicProfileResponse?> HandleAsync(Guid profileId, CancellationToken cancellationToken)
    {
        var viewerUserId = currentUser.RequireUserId();

        var profile = await dbContext.Profiles
            .AsNoTracking()
            .Include(p => p.ProfileInterests)
            .ThenInclude(pi => pi.Interest)
            .FirstOrDefaultAsync(p => p.Id == profileId, cancellationToken);
        if (profile is null)
        {
            return null;
        }

        var hasBlock = await dbContext.UserBlocks.AnyAsync(
            b => (b.BlockerUserId == profile.UserId && b.BlockedUserId == viewerUserId) ||
                 (b.BlockerUserId == viewerUserId && b.BlockedUserId == profile.UserId),
            cancellationToken);

        return profile.IsVisibleTo(viewerUserId, hasBlock)
            ? ProfileResponses.ToPublicResponse(profile)
            : null;
    }
}
