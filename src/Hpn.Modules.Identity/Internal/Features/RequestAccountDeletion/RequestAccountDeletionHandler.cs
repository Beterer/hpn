using Hpn.Modules.Identity.Internal.Domain;
using Hpn.Modules.Identity.Internal.Persistence;
using Hpn.Modules.Profile.Contracts;
using Hpn.SharedKernel.Accounts;
using Hpn.SharedKernel.Auth;
using Hpn.SharedKernel.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Hpn.Modules.Identity.Internal.Features.RequestAccountDeletion;

internal sealed record RequestAccountDeletionResult(bool UserMissing, DateTimeOffset? PurgeAfter);

/// <summary>
/// Soft-delete (phase one, §10.5): marks the account for deletion, revokes every
/// session so access stops immediately, and announces it so other modules (Profile)
/// can make the account inert. The irreversible purge happens later, after the
/// grace window, via <see cref="Accounts.AccountPurgeService"/>. Idempotent — a
/// repeated request keeps the original deadline.
/// </summary>
internal sealed class RequestAccountDeletionHandler(
    IdentityDbContext dbContext,
    ICurrentUser currentUser,
    IProfileApi profileApi,
    TimeProvider timeProvider,
    IOptions<IdentityOptions> options,
    IDomainEventDispatcher eventDispatcher)
{
    public async Task<RequestAccountDeletionResult> HandleAsync(CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return new RequestAccountDeletionResult(UserMissing: true, PurgeAfter: null);
        }

        if (user.Status == UserStatus.PendingDeletion)
        {
            return new RequestAccountDeletionResult(UserMissing: false, PurgeAfter: user.PurgeAfter);
        }

        // Capture the profile id now, while the profile still exists, so the later
        // hard purge never has to re-resolve it (it may be gone by then) — §10.5.
        var profileId = await profileApi.GetProfileIdForUserAsync(userId, cancellationToken);

        var now = timeProvider.GetUtcNow();
        user.RequestDeletion(now, options.Value.AccountDeletionGrace, profileId);

        var sessions = await dbContext.Sessions
            .Where(s => s.UserId == userId && s.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var session in sessions)
        {
            session.Revoke(now);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // Synchronous in-process fan-out (§10.7): Profile hides the account from the
        // feed at once. Dispatched after the identity write is durable.
        await eventDispatcher.DispatchAsync(new AccountDeletionRequested(userId, now), cancellationToken);

        return new RequestAccountDeletionResult(UserMissing: false, PurgeAfter: user.PurgeAfter);
    }
}
