using Hpn.Modules.Profile.Contracts;
using Hpn.Modules.Profile.Internal.Domain;
using Hpn.Modules.Profile.Internal.Features;
using Hpn.Modules.Profile.Internal.Persistence;
using Hpn.SharedKernel.Auth;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Profile.Internal.Features.UpdateVisibilitySettings;

internal sealed record UpdateVisibilitySettingsResult(
    VisibilityPreferencesResponse? Preferences,
    bool ProfileMissing,
    ProfileActivationRequirementResult? FailedRequirement);

internal sealed class UpdateVisibilitySettingsHandler(
    ProfileDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    IEnumerable<IProfileActivationRequirement> activationRequirements)
{
    public async Task<UpdateVisibilitySettingsResult> HandleAsync(
        UpdateVisibilitySettingsRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();

        // VisibilityPreferences auto-includes on the profile (see ProfileConfiguration).
        var profile = await dbContext.Profiles
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (profile is null)
        {
            return new UpdateVisibilitySettingsResult(null, ProfileMissing: true, FailedRequirement: null);
        }

        var now = timeProvider.GetUtcNow();

        // The "paused" toggle is the same concept as the status lifecycle's pause, so
        // keep them in lockstep — otherwise unticking it here would leave a
        // status-paused profile silently hidden. Pausing is always safe; un-pausing
        // re-activates and must therefore meet the same activation requirements as
        // PUT /profile/status (e.g. a ready photo).
        if (request.Paused && profile.Status == ProfileStatus.Active)
        {
            profile.Pause(now);
        }
        else if (!request.Paused && profile.Status == ProfileStatus.Paused)
        {
            foreach (var requirement in activationRequirements)
            {
                var result = await requirement.CheckAsync(profile.Id, cancellationToken);
                if (!result.Satisfied)
                {
                    return new UpdateVisibilitySettingsResult(null, ProfileMissing: false, FailedRequirement: result);
                }
            }

            profile.Activate(now);
        }

        profile.VisibilityPreferences.Update(
            request.ShowOnlyOutsideCountry,
            request.HideFromCountry,
            request.MinDistanceKm,
            request.WomenForWomen,
            request.VerifiedOnly,
            request.Paused);

        await dbContext.SaveChangesAsync(cancellationToken);

        var prefs = profile.VisibilityPreferences;
        return new UpdateVisibilitySettingsResult(
            new VisibilityPreferencesResponse(
                prefs.ShowOnlyOutsideCountry,
                prefs.HideFromCountry,
                prefs.MinDistanceKm,
                prefs.WomenForWomen,
                prefs.VerifiedOnly,
                prefs.Paused),
            ProfileMissing: false,
            FailedRequirement: null);
    }
}
