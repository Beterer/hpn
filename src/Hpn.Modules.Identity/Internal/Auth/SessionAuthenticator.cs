using System.Security.Claims;
using Hpn.Modules.Identity.Internal.Persistence;
using Hpn.Modules.Identity.Internal.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Hpn.Modules.Identity.Internal.Auth;

/// <summary>
/// Resolves a raw session cookie to a principal: looks the session up by hash,
/// checks it is active, slides its expiry when due, and projects the user's id
/// and role into claims (backbone §10.1). Returns <c>null</c> for any
/// missing/expired/revoked session so the handler reports anonymous.
/// </summary>
internal sealed class SessionAuthenticator(
    IdentityDbContext dbContext,
    TimeProvider timeProvider,
    IOptions<IdentityOptions> options)
{
    private readonly IdentityOptions _options = options.Value;

    public async Task<ClaimsPrincipal?> AuthenticateAsync(string rawToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return null;
        }

        var tokenHash = TokenHasher.Hash(rawToken);
        var session = await dbContext.Sessions
            .FirstOrDefaultAsync(s => s.TokenHash == tokenHash, cancellationToken);

        var now = timeProvider.GetUtcNow();
        if (session is null || !session.IsActive(now))
        {
            return null;
        }

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == session.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return null;
        }

        if (session.SlideIfDue(now, _options.SessionLifetime))
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var identity = new ClaimsIdentity(SessionAuthenticationDefaults.Scheme);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Role, user.Role.ToString().ToLowerInvariant()));
        return new ClaimsPrincipal(identity);
    }
}
