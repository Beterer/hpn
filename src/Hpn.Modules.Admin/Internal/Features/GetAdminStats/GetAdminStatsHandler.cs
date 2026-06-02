using Hpn.Modules.Admin.Internal.Persistence;
using Hpn.Modules.Moderation.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Admin.Internal.Features.GetAdminStats;

internal sealed class GetAdminStatsHandler(AdminDbContext dbContext, TimeProvider timeProvider)
{
    public async Task<AdminStatsResponse> HandleAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var row = await dbContext.Stats
            .FromSqlInterpolated(
                $"""
                 WITH latest_actions AS (
                     SELECT DISTINCT ON (target_user_id)
                         target_user_id, action, expires_at, created_at
                     FROM moderation.moderation_actions
                     ORDER BY target_user_id, created_at DESC
                 )
                 SELECT
                     (SELECT COUNT(*)::int FROM moderation.reports WHERE status = 'open') AS open_reports,
                     (SELECT COUNT(*)::int FROM moderation.reports WHERE status = 'reviewing') AS reviewing_reports,
                     (SELECT COUNT(*)::int FROM moderation.reports WHERE status = 'actioned') AS actioned_reports,
                     (SELECT COUNT(*)::int FROM moderation.reports WHERE status = 'dismissed') AS dismissed_reports,
                     (SELECT COUNT(*)::int FROM latest_actions WHERE action = 'temp_restrict' AND expires_at > {now}) AS currently_restricted,
                     (SELECT COUNT(*)::int FROM latest_actions WHERE action = 'ban') AS currently_banned,
                     (SELECT COUNT(*)::int FROM profile.profiles WHERE verified) AS verified_profiles,
                     -- Platform-wide average: every non-deleted profile counts, with the
                     -- base score standing in for accounts not yet scored, so the metric
                     -- isn't skewed to the moderation-touched subset that has trust rows.
                     (SELECT COALESCE(AVG(COALESCE(t.score, {ModerationDefaults.BaseTrustScore})), 0)::double precision
                      FROM profile.profiles p
                      LEFT JOIN moderation.account_trust t ON t.user_id = p.user_id
                      WHERE p.status <> 'deleted') AS average_trust_score
                 """)
            .AsNoTracking()
            .SingleAsync(cancellationToken);

        return new AdminStatsResponse(
            row.OpenReports,
            row.ReviewingReports,
            row.ActionedReports,
            row.DismissedReports,
            row.CurrentlyRestricted,
            row.CurrentlyBanned,
            row.VerifiedProfiles,
            Math.Round(row.AverageTrustScore, 3, MidpointRounding.AwayFromZero));
    }
}
