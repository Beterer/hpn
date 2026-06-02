using Hpn.Modules.Admin.Internal.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Admin.Internal.Features.GetAdminReports;

internal sealed class GetAdminReportsHandler(AdminDbContext dbContext)
{
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.Ordinal)
    {
        "open",
        "reviewing",
        "actioned",
        "dismissed",
    };

    private const int DefaultLimit = 50;
    private const int MaxLimit = 100;

    public async Task<IReadOnlyCollection<AdminReportResponse>> HandleAsync(
        string? status,
        int? requestedLimit,
        CancellationToken cancellationToken)
    {
        // An explicit but unrecognized status filter matches nothing — don't silently
        // fall back to returning every report.
        if (!string.IsNullOrWhiteSpace(status) &&
            !AllowedStatuses.Contains(status.Trim().ToLowerInvariant()))
        {
            return [];
        }

        var normalizedStatus = NormalizeStatus(status);
        var limit = Math.Clamp(requestedLimit.GetValueOrDefault(DefaultLimit), 1, MaxLimit);
        var query = normalizedStatus is null
            ? dbContext.Reports.FromSqlInterpolated(
                $"""
                 SELECT
                     r.id AS report_id,
                     r.reporter_user_id,
                     r.target_profile_id,
                     p.user_id AS target_user_id,
                     p.display_name AS target_display_name,
                     r.type,
                     r.status,
                     r.note,
                     r.created_at,
                     r.resolved_at
                 FROM moderation.reports r
                 LEFT JOIN profile.profiles p ON p.id = r.target_profile_id
                 ORDER BY r.created_at DESC, r.id DESC
                 LIMIT {limit}
                 """)
            : dbContext.Reports.FromSqlInterpolated(
                $"""
                 SELECT
                     r.id AS report_id,
                     r.reporter_user_id,
                     r.target_profile_id,
                     p.user_id AS target_user_id,
                     p.display_name AS target_display_name,
                     r.type,
                     r.status,
                     r.note,
                     r.created_at,
                     r.resolved_at
                 FROM moderation.reports r
                 LEFT JOIN profile.profiles p ON p.id = r.target_profile_id
                 WHERE r.status = {normalizedStatus}
                 ORDER BY r.created_at DESC, r.id DESC
                 LIMIT {limit}
                 """);

        var rows = await query
            .AsNoTracking()
            .ToArrayAsync(cancellationToken);

        return rows
            .Select(r => new AdminReportResponse(
                r.ReportId,
                r.ReporterUserId,
                r.TargetProfileId,
                r.TargetUserId,
                r.TargetDisplayName,
                r.Type,
                r.Status,
                r.Note,
                r.CreatedAt,
                r.ResolvedAt))
            .ToArray();
    }

    private static string? NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        var normalized = status.Trim().ToLowerInvariant();
        return AllowedStatuses.Contains(normalized) ? normalized : null;
    }
}
