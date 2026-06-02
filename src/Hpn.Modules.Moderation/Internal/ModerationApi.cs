using Hpn.Modules.Moderation.Contracts;
using Hpn.Modules.Moderation.Internal.Domain;
using Hpn.Modules.Moderation.Internal.Persistence;
using Hpn.Modules.Moderation.Internal.Trust;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Moderation.Internal;

/// <summary>
/// Read-only implementation of the cross-module Moderation contract. Writes stay in
/// the module's services/slices (§3.3).
/// </summary>
internal sealed class ModerationApi(ModerationDbContext dbContext, TimeProvider timeProvider) : IModerationApi
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
}
