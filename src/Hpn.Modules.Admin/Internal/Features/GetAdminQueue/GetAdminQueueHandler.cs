using Hpn.Modules.Admin.Internal.Persistence;
using Hpn.Modules.Moderation.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Admin.Internal.Features.GetAdminQueue;

internal sealed class GetAdminQueueHandler(AdminDbContext dbContext)
{
    private const int DefaultLimit = 25;
    private const int MaxLimit = 100;

    public async Task<IReadOnlyCollection<AdminQueueItemResponse>> HandleAsync(
        int? requestedLimit,
        CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(requestedLimit.GetValueOrDefault(DefaultLimit), 1, MaxLimit);

        var rows = await dbContext.QueueItems
            .FromSqlInterpolated(
                $"""
                 SELECT
                     r.target_profile_id AS profile_id,
                     p.user_id AS target_user_id,
                     p.display_name,
                     p.status AS profile_status,
                     COUNT(*)::int AS report_count,
                     MAX(r.created_at) AS latest_report_at,
                     COALESCE(t.score, {ModerationDefaults.BaseTrustScore})::double precision AS trust_score
                 FROM moderation.reports r
                 JOIN profile.profiles p ON p.id = r.target_profile_id
                 LEFT JOIN moderation.account_trust t ON t.user_id = p.user_id
                 WHERE r.status IN ('open', 'reviewing')
                 GROUP BY r.target_profile_id, p.user_id, p.display_name, p.status, t.score
                 ORDER BY MAX(r.created_at) DESC, r.target_profile_id
                 LIMIT {limit}
                 """)
            .AsNoTracking()
            .ToArrayAsync(cancellationToken);

        return rows
            .Select(r => new AdminQueueItemResponse(
                r.ProfileId,
                r.TargetUserId,
                r.DisplayName,
                r.ProfileStatus,
                r.ReportCount,
                r.LatestReportAt,
                Math.Round(r.TrustScore, 3, MidpointRounding.AwayFromZero)))
            .ToArray();
    }
}
