using System.Text.Json;
using Hpn.Modules.Appreciation.Contracts;
using Hpn.Modules.Identity.Contracts;
using Hpn.Modules.Moderation.Internal.Domain;
using Hpn.Modules.Moderation.Internal.Persistence;
using Hpn.Modules.Photo.Contracts;
using Hpn.Modules.Profile.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Moderation.Internal.Trust;

/// <summary>
/// Gathers the trust signals for an account from the sanctioned cross-module reads
/// (Identity age, Photo primary, Profile verified, Appreciation engagement) plus this
/// module's own upheld actions, computes the score (§10.3), and caches it in
/// <c>account_trust</c>. Recomputed at the moderation-relevant moments — a report on
/// or by the user, or an action against them — so the cached value is current
/// whenever pressure or a restriction decision needs it. All reads go through
/// Contracts; no other module's internals are touched.
/// </summary>
internal sealed class TrustScoreService(
    ModerationDbContext dbContext,
    IIdentityApi identityApi,
    IProfileApi profileApi,
    IPhotoApi photoApi,
    IAppreciationApi appreciationApi,
    TimeProvider timeProvider)
{
    /// <summary>
    /// Recomputes and persists the trust score for <paramref name="userId"/>, returning
    /// it. A user that no longer exists scores 0 and is not cached.
    /// </summary>
    public async Task<double> RecomputeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();

        var user = await identityApi.GetUserAsync(userId, cancellationToken);
        if (user is null)
        {
            return 0.0;
        }

        var profileId = await profileApi.GetProfileIdForUserAsync(userId, cancellationToken);

        var verified = profileId is { } pid && await profileApi.IsVerifiedAsync(pid, cancellationToken);

        var hasReadyPrimaryPhoto = false;
        if (profileId is { } photoProfileId)
        {
            var primary = await photoApi.GetPrimaryPhotoAsync(photoProfileId, cancellationToken);
            hasReadyPrimaryPhoto = primary is not null &&
                string.Equals(primary.Status, "ready", StringComparison.Ordinal);
        }

        var given = (await appreciationApi.GetAppreciationStyleAsync(userId, cancellationToken)).Total;
        var received = profileId is { } receiverId
            ? (await appreciationApi.GetReceivedSummaryAsync(receiverId, cancellationToken)).Total
            : 0;

        var upheldActions = await dbContext.ModerationActions
            .AsNoTracking()
            .CountAsync(
                a => a.TargetUserId == userId &&
                     (a.Action == ActionType.Ban || a.Action == ActionType.TempRestrict),
                cancellationToken);

        var signals = new TrustSignals(
            AccountAgeDays: (now - user.CreatedAt).TotalDays,
            HasReadyPrimaryPhoto: hasReadyPrimaryPhoto,
            Verified: verified,
            GivenAppreciations: given,
            ReceivedAppreciations: received,
            UpheldActions: upheldActions);

        var score = TrustScoreCalculator.Compute(signals);
        var signalsJson = JsonSerializer.Serialize(signals);

        // Upsert atomically (same posture as the appreciation counter projection): two
        // reports touching the same user's trust row concurrently must not race a
        // find-then-insert into a duplicate-key failure — the row is keyed by user_id.
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO moderation.account_trust (user_id, score, signals, updated_at)
             VALUES ({userId}, {score}, {signalsJson}::jsonb, {now})
             ON CONFLICT (user_id)
             DO UPDATE SET
                 score = EXCLUDED.score,
                 signals = EXCLUDED.signals,
                 updated_at = EXCLUDED.updated_at
             """,
            cancellationToken);

        return score;
    }
}
