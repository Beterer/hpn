using Hpn.Modules.Moderation.Internal.Domain;
using Hpn.Modules.Moderation.Internal.Persistence;
using Hpn.Modules.Moderation.Internal.Trust;
using Hpn.Modules.Profile.Contracts;
using Hpn.SharedKernel.Auth;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Hpn.Modules.Moderation.Internal.Features.SubmitReport;

internal sealed record SubmitReportResult(
    SubmitReportResponse? Response,
    bool TargetMissing,
    bool SelfReport)
{
    public static SubmitReportResult Ok(Guid reportId) =>
        new(new SubmitReportResponse(reportId, "received"), TargetMissing: false, SelfReport: false);

    public static readonly SubmitReportResult Missing = new(null, TargetMissing: true, SelfReport: false);
    public static readonly SubmitReportResult Self = new(null, TargetMissing: false, SelfReport: true);
}

/// <summary>
/// Intake for a report (backbone §6.7, §10.3). It persists the report (collapsing
/// duplicates), refreshes the reporter's trust so their weight is current, then
/// re-evaluates weighted report pressure on the target — which may apply an automatic
/// temporary restriction. It never bans and never tells the reporter what happened.
/// </summary>
internal sealed class SubmitReportHandler(
    ModerationDbContext dbContext,
    IProfileApi profileApi,
    ICurrentUser currentUser,
    TrustScoreService trustService,
    ReportPressureEvaluator pressureEvaluator,
    TimeProvider timeProvider)
{
    public async Task<SubmitReportResult> HandleAsync(SubmitReportRequest request, CancellationToken cancellationToken)
    {
        var reporterUserId = currentUser.RequireUserId();
        var type = ModerationFormat.ParseReportType(request.Type);

        var targetUserId = await profileApi.GetUserIdForProfileAsync(request.TargetProfileId, cancellationToken);
        if (targetUserId is not { } targetUser)
        {
            return SubmitReportResult.Missing;
        }

        if (targetUser == reporterUserId)
        {
            return SubmitReportResult.Self;
        }

        // Duplicate reports on the same target/type collapse (§10.3): re-reporting is
        // idempotent and does not re-trigger pressure.
        var existing = await dbContext.Reports
            .AsNoTracking()
            .Where(r => r.ReporterUserId == reporterUserId &&
                        r.TargetProfileId == request.TargetProfileId &&
                        r.Type == type)
            .Select(r => (Guid?)r.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (existing is { } existingId)
        {
            return SubmitReportResult.Ok(existingId);
        }

        var now = timeProvider.GetUtcNow();
        var report = Report.Create(reporterUserId, request.TargetProfileId, type, request.Note, now);
        dbContext.Reports.Add(report);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Raced with another submit of the same (reporter, target, type) — collapse.
            dbContext.Entry(report).State = EntityState.Detached;
            var raced = await dbContext.Reports
                .AsNoTracking()
                .Where(r => r.ReporterUserId == reporterUserId &&
                            r.TargetProfileId == request.TargetProfileId &&
                            r.Type == type)
                .Select(r => r.Id)
                .FirstAsync(cancellationToken);
            return SubmitReportResult.Ok(raced);
        }

        // Keep the reporter's trust current so they're weighted correctly in pressure.
        await trustService.RecomputeAsync(reporterUserId, cancellationToken);

        // A new distinct report may push the target over the auto-restriction threshold.
        await pressureEvaluator.EvaluateAsync(targetUser, request.TargetProfileId, cancellationToken);

        return SubmitReportResult.Ok(report.Id);
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
