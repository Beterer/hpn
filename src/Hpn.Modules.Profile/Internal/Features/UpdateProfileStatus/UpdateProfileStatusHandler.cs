using Hpn.Modules.Profile.Contracts.Events;
using Hpn.Modules.Profile.Internal.Features;
using Hpn.Modules.Profile.Internal.Persistence;
using Hpn.SharedKernel.Auth;
using Hpn.SharedKernel.Events;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Profile.Internal.Features.UpdateProfileStatus;

internal sealed record UpdateProfileStatusResult(
    ProfileResponse? Profile,
    bool ProfileMissing,
    bool InvalidTransition);

internal sealed class UpdateProfileStatusHandler(
    ProfileDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    IDomainEventDispatcher eventDispatcher)
{
    public async Task<UpdateProfileStatusResult> HandleAsync(
        UpdateProfileStatusRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        var profile = await dbContext.Profiles
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (profile is null)
        {
            return new UpdateProfileStatusResult(null, ProfileMissing: true, InvalidTransition: false);
        }

        var now = timeProvider.GetUtcNow();
        var changed = request.Status switch
        {
            "active" => profile.Activate(now),
            "paused" => profile.Pause(now),
            _ => false,
        };

        if (!changed)
        {
            return new UpdateProfileStatusResult(null, ProfileMissing: false, InvalidTransition: true);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (request.Status == "active")
        {
            await eventDispatcher.DispatchAsync(new ProfileActivated(profile.Id, profile.UserId, now), cancellationToken);
        }
        else
        {
            await eventDispatcher.DispatchAsync(new ProfilePaused(profile.Id, profile.UserId, now), cancellationToken);
        }

        var response = await ProfileResponses.LoadMineAsync(dbContext, userId, cancellationToken)
            ?? throw new InvalidOperationException("Profile was saved but could not be reloaded.");

        return new UpdateProfileStatusResult(response, ProfileMissing: false, InvalidTransition: false);
    }
}
