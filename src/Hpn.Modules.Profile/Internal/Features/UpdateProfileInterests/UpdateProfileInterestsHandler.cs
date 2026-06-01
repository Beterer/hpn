using Hpn.Modules.Profile.Internal.Features;
using Hpn.Modules.Profile.Internal.Persistence;
using Hpn.SharedKernel.Auth;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Profile.Internal.Features.UpdateProfileInterests;

internal sealed record UpdateProfileInterestsResult(
    ProfileResponse? Profile,
    bool ProfileMissing,
    bool UnknownInterest);

internal sealed class UpdateProfileInterestsHandler(
    ProfileDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
{
    public async Task<UpdateProfileInterestsResult> HandleAsync(
        UpdateProfileInterestsRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        var profile = await dbContext.Profiles
            .Include(p => p.ProfileInterests)
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (profile is null)
        {
            return new UpdateProfileInterestsResult(null, ProfileMissing: true, UnknownInterest: false);
        }

        var interestIds = request.InterestIds.Distinct().ToArray();
        var interests = await dbContext.Interests
            .Where(i => interestIds.Contains(i.Id))
            .ToArrayAsync(cancellationToken);
        if (interests.Length != interestIds.Length)
        {
            return new UpdateProfileInterestsResult(null, ProfileMissing: false, UnknownInterest: true);
        }

        profile.ReplaceInterests(interests, timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await ProfileResponses.LoadMineAsync(dbContext, userId, cancellationToken)
            ?? throw new InvalidOperationException("Profile was saved but could not be reloaded.");

        return new UpdateProfileInterestsResult(response, ProfileMissing: false, UnknownInterest: false);
    }
}
