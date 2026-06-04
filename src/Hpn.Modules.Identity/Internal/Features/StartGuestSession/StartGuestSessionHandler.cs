using Hpn.Modules.Identity.Internal.Auth;
using Hpn.Modules.Identity.Internal.Domain;
using Hpn.Modules.Identity.Internal.Persistence;
using Hpn.Modules.Identity.Internal.Security;
using Microsoft.Extensions.Options;

namespace Hpn.Modules.Identity.Internal.Features.StartGuestSession;

internal sealed record StartGuestSessionResult(
    StartGuestSessionResponse Response,
    string? GuestToken,
    DateTimeOffset? ExpiresAt);

internal sealed class StartGuestSessionHandler(
    IdentityDbContext dbContext,
    GuestSessionAuthenticator authenticator,
    TimeProvider timeProvider,
    IOptions<IdentityOptions> options)
{
    private readonly IdentityOptions _options = options.Value;

    public async Task<StartGuestSessionResult> HandleAsync(
        string? existingGuestToken,
        string? userAgent,
        string? ip,
        CancellationToken cancellationToken)
    {
        var existing = await authenticator.FindActiveSessionAsync(existingGuestToken, cancellationToken);
        if (existing is not null)
        {
            return new StartGuestSessionResult(ToResponse(), GuestToken: null, ExpiresAt: null);
        }

        var now = timeProvider.GetUtcNow();
        var guestToken = SecureTokenGenerator.Generate();
        var session = GuestSession.Start(
            TokenHasher.Hash(guestToken),
            now,
            _options.GuestSessionLifetime,
            userAgent,
            ip);

        dbContext.GuestSessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new StartGuestSessionResult(ToResponse(), guestToken, session.ExpiresAt);
    }

    private StartGuestSessionResponse ToResponse() =>
        new(Math.Max(1, _options.NudgeReactionThreshold));
}
