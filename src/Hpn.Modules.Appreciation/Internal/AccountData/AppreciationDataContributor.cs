using Hpn.Modules.Appreciation.Internal.Persistence;
using Hpn.SharedKernel.Accounts;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Appreciation.Internal.AccountData;

/// <summary>
/// Appreciation's slice of account export + erasure (§10.5). An account appears
/// here in two roles: as a <em>sender</em> (keyed by user id — appreciations it
/// gave) and as a <em>receiver</em> (keyed by profile id — appreciations it got).
/// Both are exported and both are purged.
/// </summary>
internal sealed class AppreciationDataContributor(AppreciationDbContext dbContext) : IAccountDataContributor
{
    public string Section => "appreciations";

    public async Task<object?> ExportAsync(AccountScope scope, CancellationToken cancellationToken = default)
    {
        var given = await dbContext.GivenAppreciationStats
            .AsNoTracking()
            .Where(s => s.SenderUserId == scope.UserId)
            .Select(s => new { s.CategoryId, s.Count })
            .ToArrayAsync(cancellationToken);

        var received = scope.ProfileId is { } profileId
            ? await dbContext.ReceivedAppreciationStats
                .AsNoTracking()
                .Where(s => s.ReceiverProfileId == profileId)
                .Select(s => new { s.CategoryId, s.Count, s.LastAt })
                .ToArrayAsync(cancellationToken)
            : [];

        if (given.Length == 0 && received.Length == 0)
        {
            return null;
        }

        return new { Given = given, Received = received };
    }

    public async Task EraseAsync(AccountScope scope, CancellationToken cancellationToken = default)
    {
        await dbContext.GivenAppreciationStats
            .Where(s => s.SenderUserId == scope.UserId)
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.AppreciationEvents
            .Where(e => e.SenderUserId == scope.UserId)
            .ExecuteDeleteAsync(cancellationToken);

        if (scope.ProfileId is { } profileId)
        {
            await dbContext.ReceivedAppreciationStats
                .Where(s => s.ReceiverProfileId == profileId)
                .ExecuteDeleteAsync(cancellationToken);

            await dbContext.AppreciationEvents
                .Where(e => e.ReceiverProfileId == profileId)
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
