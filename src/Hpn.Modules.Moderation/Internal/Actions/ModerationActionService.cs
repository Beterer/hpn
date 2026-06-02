using Hpn.Modules.Moderation.Internal.Domain;
using Hpn.Modules.Moderation.Internal.Persistence;
using Hpn.SharedKernel.Events;
using Hpn.SharedKernel.Moderation;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Moderation.Internal.Actions;

/// <summary>
/// The single path that records a moderation decision: every restriction, ban and
/// clear is written to <c>moderation_actions</c> here and nowhere else (backbone
/// §6.7, §10.3). After the write is durable it raises the matching shared event so
/// Profile can move the account in/out of the feed. The system applies only
/// <see cref="RestrictAsync"/> automatically; bans and clears are admin/system
/// decisions (wired to admin endpoints in M10) — never an automatic consequence of
/// report volume.
/// </summary>
internal sealed class ModerationActionService(
    ModerationDbContext dbContext,
    IDomainEventDispatcher eventDispatcher)
{
    /// <summary>How long an automatic temporary restriction lasts (§10.3, tunable).</summary>
    public static readonly TimeSpan RestrictionWindow = TimeSpan.FromHours(48);

    /// <summary>
    /// Records a warning and resolves the target's outstanding reports as handled. It
    /// does not change feed eligibility, but it does clear the profile from the review
    /// queue (which lists open/reviewing reports) — a warning is a decision taken.
    /// </summary>
    public async Task WarnAsync(
        Guid targetUserId,
        Guid targetProfileId,
        string reason,
        string actor,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        dbContext.ModerationActions.Add(ModerationAction.Warn(targetUserId, reason, actor, now));

        var openReports = await dbContext.Reports
            .Where(r => r.TargetProfileId == targetProfileId &&
                        (r.Status == ReportStatus.Open || r.Status == ReportStatus.Reviewing))
            .ToListAsync(cancellationToken);
        foreach (var report in openReports)
        {
            report.MarkActioned(now);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Applies a temporary restriction and enqueues the target's open reports for
    /// review. Always temporary — the account returns automatically when the window
    /// elapses (see <see cref="RestrictionExpiryService"/>).
    /// </summary>
    public async Task RestrictAsync(
        Guid targetUserId,
        Guid targetProfileId,
        string reason,
        string actor,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var expiresAt = now + RestrictionWindow;
        dbContext.ModerationActions.Add(
            ModerationAction.TempRestrict(targetUserId, reason, actor, now, expiresAt));

        var openReports = await dbContext.Reports
            .Where(r => r.TargetProfileId == targetProfileId && r.Status == ReportStatus.Open)
            .ToListAsync(cancellationToken);
        foreach (var report in openReports)
        {
            report.MarkReviewing();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await eventDispatcher.DispatchAsync(new UserRestricted(targetUserId, expiresAt, now), cancellationToken);
    }

    /// <summary>
    /// Bans an account (admin/system decision) and resolves its outstanding reports as
    /// upheld. A ban is permanent until an explicit <see cref="ClearAsync"/>.
    /// </summary>
    public async Task BanAsync(
        Guid targetUserId,
        Guid targetProfileId,
        string reason,
        string actor,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        dbContext.ModerationActions.Add(ModerationAction.Ban(targetUserId, reason, actor, now));

        var reports = await dbContext.Reports
            .Where(r => r.TargetProfileId == targetProfileId &&
                        (r.Status == ReportStatus.Open || r.Status == ReportStatus.Reviewing))
            .ToListAsync(cancellationToken);
        foreach (var report in reports)
        {
            report.MarkActioned(now);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await eventDispatcher.DispatchAsync(new UserBanned(targetUserId, now), cancellationToken);
    }

    /// <summary>
    /// Lifts a restriction or ban — by an admin clear, or automatically on expiry. The
    /// account returns to the feed. Outstanding reports are left in the queue for an
    /// admin to resolve unless <paramref name="dismissReports"/> is set (an explicit
    /// "no action" decision).
    /// </summary>
    public async Task ClearAsync(
        Guid targetUserId,
        Guid? targetProfileId,
        string reason,
        string actor,
        DateTimeOffset now,
        bool dismissReports,
        CancellationToken cancellationToken = default)
    {
        dbContext.ModerationActions.Add(ModerationAction.Clear(targetUserId, reason, actor, now));

        if (dismissReports && targetProfileId is { } profileId)
        {
            var reports = await dbContext.Reports
                .Where(r => r.TargetProfileId == profileId &&
                            (r.Status == ReportStatus.Open || r.Status == ReportStatus.Reviewing))
                .ToListAsync(cancellationToken);
            foreach (var report in reports)
            {
                report.MarkDismissed(now);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await eventDispatcher.DispatchAsync(new UserCleared(targetUserId, now), cancellationToken);
    }
}
