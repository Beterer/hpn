using Hpn.Modules.Identity.Internal.Persistence;
using Hpn.Modules.Identity.Internal.Security;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Identity.Internal.Features.Logout;

/// <summary>
/// Revokes the session backing the current cookie (backbone §10.1: sessions are
/// revocable). Idempotent — an already-revoked or unknown token is a no-op.
/// </summary>
internal sealed class LogoutHandler(IdentityDbContext dbContext, TimeProvider timeProvider)
{
    public async Task HandleAsync(string? rawToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(rawToken))
        {
            return;
        }

        var tokenHash = TokenHasher.Hash(rawToken);
        var session = await dbContext.Sessions.FirstOrDefaultAsync(s => s.TokenHash == tokenHash, cancellationToken);
        if (session is null)
        {
            return;
        }

        session.Revoke(timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
