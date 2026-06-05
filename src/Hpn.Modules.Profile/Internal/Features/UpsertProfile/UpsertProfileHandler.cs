using Hpn.Modules.Profile.Internal.Domain;
using Hpn.Modules.Profile.Internal.Features;
using Hpn.Modules.Profile.Internal.Persistence;
using Hpn.SharedKernel.Auth;
using Hpn.SharedKernel.Geo;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Profile.Internal.Features.UpsertProfile;

internal sealed class UpsertProfileHandler(
    ProfileDbContext dbContext,
    ICurrentUser currentUser,
    IClientCountryResolver countryResolver,
    TimeProvider timeProvider)
{
    public async Task<ProfileResponse> HandleAsync(UpsertProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        var now = timeProvider.GetUtcNow();
        var gender = ProfileFormat.ParseGender(request.Gender);

        var profile = await dbContext.Profiles
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (profile is null)
        {
            profile = UserProfile.Create(
                userId,
                request.DisplayName,
                gender,
                request.SelfDescribeText,
                now);
            dbContext.Profiles.Add(profile);
        }
        else
        {
            profile.UpdateDetails(
                request.DisplayName,
                gender,
                request.SelfDescribeText,
                now);
        }

        // Country is derived from the request edge, not the form — used only for the
        // same-country privacy filter, never shown (ADR-028). A null signal keeps any
        // existing value.
        profile.SetCountry(countryResolver.ResolveCountryCode(), now);

        await dbContext.SaveChangesAsync(cancellationToken);
        return await ProfileResponses.LoadMineAsync(dbContext, userId, cancellationToken)
            ?? throw new InvalidOperationException("Profile was saved but could not be reloaded.");
    }
}
