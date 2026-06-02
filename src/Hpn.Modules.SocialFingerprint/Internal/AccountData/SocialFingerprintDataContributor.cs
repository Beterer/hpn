using Hpn.Modules.SocialFingerprint.Internal.Persistence;
using Hpn.SharedKernel.Accounts;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.SocialFingerprint.Internal.AccountData;

/// <summary>
/// Social Fingerprint's slice of account export + erasure (§10.5): the opportunistic
/// weekly snapshots, keyed by profile id.
/// </summary>
internal sealed class SocialFingerprintDataContributor(SocialFingerprintDbContext dbContext) : IAccountDataContributor
{
    public string Section => "socialFingerprint";

    public async Task<object?> ExportAsync(AccountScope scope, CancellationToken cancellationToken = default)
    {
        if (scope.ProfileId is not { } profileId)
        {
            return null;
        }

        var snapshots = await dbContext.SocialFingerprintSnapshots
            .AsNoTracking()
            .Where(s => s.ProfileId == profileId)
            .OrderBy(s => s.PeriodStart)
            .Select(s => new { s.Period, s.PeriodStart, s.SampleSize, s.CreatedAt })
            .ToArrayAsync(cancellationToken);

        return snapshots.Length == 0 ? null : snapshots;
    }

    public async Task EraseAsync(AccountScope scope, CancellationToken cancellationToken = default)
    {
        if (scope.ProfileId is not { } profileId)
        {
            return;
        }

        await dbContext.SocialFingerprintSnapshots
            .Where(s => s.ProfileId == profileId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
