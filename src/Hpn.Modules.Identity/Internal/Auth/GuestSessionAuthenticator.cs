using System.Security.Claims;
using Hpn.Modules.Identity.Internal.Domain;
using Hpn.Modules.Identity.Internal.Persistence;
using Hpn.Modules.Identity.Internal.Security;
using Hpn.SharedKernel.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Hpn.Modules.Identity.Internal.Auth;

internal sealed class GuestSessionAuthenticator(
    IdentityDbContext dbContext,
    TimeProvider timeProvider,
    IOptions<IdentityOptions> options)
{
    private readonly IdentityOptions _options = options.Value;

    public async Task<ClaimsPrincipal?> AuthenticateAsync(string rawToken, CancellationToken cancellationToken)
    {
        var guest = await FindActiveSessionAsync(rawToken, slideIfDue: true, cancellationToken);
        if (guest is null)
        {
            return null;
        }

        var identity = new ClaimsIdentity(SessionAuthenticationDefaults.Scheme);
        identity.AddClaim(new Claim(ActorClaims.KindClaimType, ActorClaims.GuestKindValue));
        identity.AddClaim(new Claim(ActorClaims.IdClaimType, guest.Id.ToString()));
        return new ClaimsPrincipal(identity);
    }

    public Task<GuestSession?> FindActiveSessionAsync(
        string? rawToken,
        CancellationToken cancellationToken) =>
        FindActiveSessionAsync(rawToken, slideIfDue: false, cancellationToken);

    private async Task<GuestSession?> FindActiveSessionAsync(
        string? rawToken,
        bool slideIfDue,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return null;
        }

        var tokenHash = TokenHasher.Hash(rawToken);
        var session = await dbContext.GuestSessions
            .FirstOrDefaultAsync(s => s.TokenHash == tokenHash, cancellationToken);

        var now = timeProvider.GetUtcNow();
        if (session is null || !session.IsActive(now))
        {
            return null;
        }

        if (slideIfDue && session.SlideIfDue(now, _options.GuestSessionLifetime))
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return session;
    }
}
