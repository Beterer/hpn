using Hpn.Modules.Profile.Internal.Persistence;
using Hpn.SharedKernel.Auth;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Profile.Internal.Features.UpdateLocation;

internal sealed record UpdateLocationResult(LocationResponse? Location, bool ProfileMissing);

internal sealed class UpdateLocationHandler(
    ProfileDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
{
    public async Task<UpdateLocationResult> HandleAsync(
        UpdateLocationRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        var profile = await dbContext.Profiles
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (profile is null)
        {
            return new UpdateLocationResult(null, ProfileMissing: true);
        }

        profile.SetLocation(request.Latitude, request.Longitude, request.Consent, timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);

        return new UpdateLocationResult(
            new LocationResponse(profile.LocationConsent, profile.GeoLat is not null),
            ProfileMissing: false);
    }
}
