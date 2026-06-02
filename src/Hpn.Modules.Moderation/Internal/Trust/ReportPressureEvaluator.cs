using Hpn.Modules.Moderation.Internal.Actions;
using Hpn.Modules.Moderation.Internal.Domain;
using Hpn.Modules.Moderation.Internal.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Moderation.Internal.Trust;

/// <summary>
/// Decides whether the reports against a target warrant an automatic temporary
/// restriction (backbone §10.3). It is deliberately not a raw count: over a 7-day
/// window it sums <em>distinct</em> reporters weighted by each reporter's trust, so a
/// trusted reporter counts for more and a low-trust one barely moves the needle. The
/// trigger compares that pressure against a target-trust-scaled threshold and also
/// requires a floor of distinct reporters. It only ever applies a temporary
/// restriction plus review — a ban is never automatic. Constants are launch defaults.
/// </summary>
internal sealed class ReportPressureEvaluator(
    ModerationDbContext dbContext,
    TrustScoreService trustService,
    ModerationActionService actionService,
    TimeProvider timeProvider)
{
    public static readonly TimeSpan Window = TimeSpan.FromDays(7);
    public const double PressureBase = 1.5;
    public const double PressureTrustFactor = 2.0;
    public const int MinDistinctReporters = 3;

    /// <summary>
    /// Re-evaluates pressure on a target after a new report and applies a temporary
    /// restriction if the threshold is crossed. No-op if the target is already
    /// restricted or banned. Returns whether a restriction was applied.
    /// </summary>
    public async Task<bool> EvaluateAsync(
        Guid targetUserId,
        Guid targetProfileId,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();

        // Don't pile a second restriction on an account that's already restricted/banned.
        if (await IsAlreadyRestrictedOrBannedAsync(targetUserId, now, cancellationToken))
        {
            return false;
        }

        var since = now - Window;
        var reporterIds = await dbContext.Reports
            .AsNoTracking()
            .Where(r => r.TargetProfileId == targetProfileId && r.CreatedAt >= since)
            .Select(r => r.ReporterUserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (reporterIds.Count < MinDistinctReporters)
        {
            return false;
        }

        // Current trust reflects the target's existing upheld actions (§10.3).
        var targetTrust = await trustService.RecomputeAsync(targetUserId, cancellationToken);

        var reporterTrust = await dbContext.AccountTrust
            .AsNoTracking()
            .Where(t => reporterIds.Contains(t.UserId))
            .ToDictionaryAsync(t => t.UserId, t => t.Score, cancellationToken);

        // Each reporter recomputes their own trust when they file, so a missing row is
        // a defensive fallback only — treat it as the neutral base.
        var pressure = reporterIds.Sum(id => reporterTrust.GetValueOrDefault(id, TrustScoreCalculator.Base));

        var threshold = PressureBase + PressureTrustFactor * targetTrust;
        if (pressure < threshold)
        {
            return false;
        }

        await actionService.RestrictAsync(
            targetUserId,
            targetProfileId,
            reason: $"Auto temp-restriction: report pressure {pressure:0.00} ≥ threshold {threshold:0.00} " +
                    $"across {reporterIds.Count} distinct reporters (§10.3).",
            actor: ModerationAction.SystemActor,
            now,
            cancellationToken);
        return true;
    }

    private async Task<bool> IsAlreadyRestrictedOrBannedAsync(
        Guid targetUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var latest = await dbContext.ModerationActions
            .AsNoTracking()
            .Where(a => a.TargetUserId == targetUserId)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return latest is not null && (latest.Action == ActionType.Ban || latest.IsActiveRestrictionAt(now));
    }
}
