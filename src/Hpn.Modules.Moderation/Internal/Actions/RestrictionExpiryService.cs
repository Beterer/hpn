using Hpn.Modules.Moderation.Internal.Domain;
using Hpn.Modules.Moderation.Internal.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hpn.Modules.Moderation.Internal.Actions;

/// <summary>
/// Releases accounts whose temporary restriction window has elapsed (backbone §10.3).
/// A restriction is always temporary, but v1 has no background worker (§12), so the
/// release is an explicit, gated maintenance step — the same posture as the account
/// purge and as production migrations. Tests drive it directly with the clock
/// advanced. An account is released only if an expired temp-restriction is still its
/// <em>latest</em> action (a later ban or clear means it must not be auto-cleared).
/// </summary>
internal sealed class RestrictionExpiryService(
    ModerationDbContext dbContext,
    ModerationActionService actionService,
    ILogger<RestrictionExpiryService> logger)
{
    /// <summary>
    /// Clears every restriction whose window has passed, returning the count released.
    /// Each account is isolated: one failure is logged and skipped so it can't stall
    /// the batch and is retried next run.
    /// </summary>
    public async Task<int> ReleaseExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var candidates = await dbContext.ModerationActions
            .AsNoTracking()
            .Where(a => a.Action == ActionType.TempRestrict && a.ExpiresAt != null && a.ExpiresAt <= now)
            .Select(a => a.TargetUserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var released = 0;
        foreach (var targetUserId in candidates)
        {
            try
            {
                var latest = await dbContext.ModerationActions
                    .AsNoTracking()
                    .Where(a => a.TargetUserId == targetUserId)
                    .OrderByDescending(a => a.CreatedAt)
                    .FirstAsync(cancellationToken);

                // Skip if a later decision (ban, or a fresh restriction) superseded it.
                if (latest.Action != ActionType.TempRestrict || latest.ExpiresAt is not { } expires || expires > now)
                {
                    continue;
                }

                await actionService.ClearAsync(
                    targetUserId,
                    targetProfileId: null,
                    reason: "Temporary restriction expired.",
                    actor: ModerationAction.SystemActor,
                    now,
                    dismissReports: false,
                    cancellationToken);
                released++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to release restriction for {UserId}; it will be retried.", targetUserId);
            }
        }

        return released;
    }
}
