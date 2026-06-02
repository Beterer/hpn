using Hpn.Modules.Moderation.Contracts;
using Hpn.Modules.Moderation.Internal.Actions;
using Hpn.Modules.Moderation.Internal.Domain;
using Hpn.Modules.Moderation.Internal.Persistence;
using Hpn.Modules.Moderation.Internal.Trust;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Moderation.Internal;

/// <summary>
/// Read-only implementation of the cross-module Moderation contract. Writes stay in
/// the module's services/slices (§3.3).
/// </summary>
internal sealed class ModerationApi(
    ModerationDbContext dbContext,
    ModerationActionService actionService,
    TrustScoreService trustScoreService,
    TimeProvider timeProvider) : IModerationApi
{
    public async Task<double> GetTrustScoreAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var score = await dbContext.AccountTrust
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .Select(t => (double?)t.Score)
            .FirstOrDefaultAsync(cancellationToken);

        return score ?? TrustScoreCalculator.Base;
    }

    public async Task<bool> IsRestrictedAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var latest = await dbContext.ModerationActions
            .AsNoTracking()
            .Where(a => a.TargetUserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return latest is not null && (latest.Action == ActionType.Ban || latest.IsActiveRestrictionAt(now));
    }

    public async Task<ModerationDecisionDto> ApplyAdminProfileActionAsync(
        Guid targetProfileId,
        Guid targetUserId,
        string action,
        string reason,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var actor = adminUserId.ToString("D");
        var normalized = action.Trim().ToLowerInvariant();

        DateTimeOffset? expiresAt = null;
        switch (normalized)
        {
            case ModerationActions.Warn:
                await actionService.WarnAsync(targetUserId, targetProfileId, reason, actor, now, cancellationToken);
                break;
            case ModerationActions.TempRestrict:
                expiresAt = now + ModerationActionService.RestrictionWindow;
                await actionService.RestrictAsync(targetUserId, targetProfileId, reason, actor, now, cancellationToken);
                break;
            case ModerationActions.Ban:
                await actionService.BanAsync(targetUserId, targetProfileId, reason, actor, now, cancellationToken);
                break;
            case ModerationActions.Clear:
                await actionService.ClearAsync(
                    targetUserId,
                    targetProfileId,
                    reason,
                    actor,
                    now,
                    dismissReports: true,
                    cancellationToken);
                break;
            default:
                // Callers must validate against ModerationActions.All first (the admin
                // endpoint does); reaching here is a programming error, not bad input.
                throw new ArgumentException(
                    $"Unknown moderation action '{action}'. Expected one of: {string.Join(", ", ModerationActions.All)}.",
                    nameof(action));
        }

        await trustScoreService.RecomputeAsync(targetUserId, cancellationToken);
        return new ModerationDecisionDto(targetProfileId, targetUserId, normalized, expiresAt);
    }
}
