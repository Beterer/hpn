using Hpn.Modules.Identity.Internal.Domain;
using Hpn.Modules.Identity.Internal.Persistence;
using Hpn.SharedKernel.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hpn.Modules.Identity.Internal.Accounts;

/// <summary>
/// Runs the second phase of deletion: the irreversible hard purge of accounts whose
/// grace window has elapsed (§10.5). v1 has no background worker (§12), so this is
/// invoked as an explicit, gated maintenance step — the same posture as production
/// migrations, which are also a deliberate deploy action rather than something that
/// fires on its own. Tests drive it directly.
/// </summary>
internal sealed class AccountPurgeService(
    IdentityDbContext dbContext,
    AccountDataOrchestrator orchestrator,
    ILogger<AccountPurgeService> logger)
{
    /// <summary>
    /// Purges every account whose <c>PurgeAfter</c> has passed. Returns the count
    /// fully purged. Each account is isolated: one account's failure is logged and
    /// skipped so it can never stall the rest of the batch, and the account stays
    /// <c>PendingDeletion</c> to be retried on the next run.
    /// </summary>
    public async Task<int> PurgeDueAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var due = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.Status == UserStatus.PendingDeletion && u.PurgeAfter != null && u.PurgeAfter <= now)
            .Select(u => new { u.Id, u.PendingDeletionProfileId })
            .ToArrayAsync(cancellationToken);

        var purged = 0;
        foreach (var account in due)
        {
            try
            {
                await orchestrator.EraseAsync(
                    new AccountScope(account.Id, account.PendingDeletionProfileId), cancellationToken);
                purged++;
            }
            catch (Exception ex)
            {
                // Isolate: log and move on so one bad account can't stall the batch.
                // It stays PendingDeletion and is retried next run.
                logger.LogError(ex, "Failed to purge account {UserId}; it will be retried.", account.Id);
            }
        }

        return purged;
    }
}
