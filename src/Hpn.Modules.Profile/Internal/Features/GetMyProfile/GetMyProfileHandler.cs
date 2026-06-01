using Hpn.Modules.Profile.Internal.Features;
using Hpn.Modules.Profile.Internal.Persistence;
using Hpn.SharedKernel.Auth;

namespace Hpn.Modules.Profile.Internal.Features.GetMyProfile;

internal sealed class GetMyProfileHandler(ProfileDbContext dbContext, ICurrentUser currentUser)
{
    public async Task<ProfileResponse?> HandleAsync(CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        return await ProfileResponses.LoadMineAsync(dbContext, userId, cancellationToken);
    }
}
