using Hpn.Modules.Identity.Internal.Domain;
using Hpn.Modules.Identity.Internal.Persistence;
using Hpn.Modules.Identity.Internal.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Hpn.Modules.Identity.Internal.Features.VerifyMagicLink;

/// <summary>Outcome of a successful verification: the user plus the freshly minted session secret.</summary>
internal sealed record VerifySuccess(AuthUserDto User, string SessionToken, DateTimeOffset ExpiresAt);

/// <summary>
/// Consumes a magic-link token and opens a server-side session (backbone §10.1).
/// Token lookup, single-use marking, and session creation happen in one
/// transaction; an expired, consumed, or unknown token yields <c>null</c> so the
/// endpoint can return a uniform 400 without leaking which case it was.
/// </summary>
internal sealed class VerifyMagicLinkHandler(
    IdentityDbContext dbContext,
    TimeProvider timeProvider,
    IOptions<IdentityOptions> options)
{
    private readonly IdentityOptions _options = options.Value;

    public async Task<VerifySuccess?> HandleAsync(
        VerifyMagicLinkRequest request,
        string? userAgent,
        string? ip,
        CancellationToken cancellationToken)
    {
        var tokenHash = TokenHasher.Hash(request.Token);
        var token = await dbContext.MagicLinkTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        var now = timeProvider.GetUtcNow();
        if (token is null || !token.IsRedeemable(now))
        {
            return null;
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == token.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return null;
        }

        token.Consume(now);
        user.RecordLogin(now);

        var sessionToken = SecureTokenGenerator.Generate();
        var session = Session.Start(user.Id, TokenHasher.Hash(sessionToken), now, _options.SessionLifetime, userAgent, ip);
        dbContext.Sessions.Add(session);

        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = new AuthUserDto(user.Id, user.Email, user.Role.ToString().ToLowerInvariant());
        return new VerifySuccess(dto, sessionToken, session.ExpiresAt);
    }
}
