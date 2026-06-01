using Hpn.Modules.Identity.Internal.Persistence;
using Hpn.SharedKernel.Auth;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Identity.Internal.Features.GetMe;

/// <summary>
/// Projects the current account + onboarding state for <c>GET /me</c>. Returns
/// <c>null</c> if the authenticated user can't be found (e.g. deleted mid-session)
/// so the endpoint degrades to 401 (backbone §10.1).
/// </summary>
internal sealed class GetMeHandler(IdentityDbContext dbContext, ICurrentUser currentUser)
{
    public async Task<MeResponse?> HandleAsync(CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();

        var user = await dbContext.Users
            .Where(u => u.Id == userId)
            .Select(u => new AuthUserDto(u.Id, u.Email, u.Role.ToString().ToLowerInvariant()))
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return null;
        }

        // M1: no profile concept yet — the next step is always "create your profile".
        var onboarding = new OnboardingDto(ProfileCreated: false, ProfileActive: false, NextStep: "create_profile");
        return new MeResponse(user, onboarding);
    }
}
