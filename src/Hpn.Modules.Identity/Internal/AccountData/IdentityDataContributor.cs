using Hpn.Modules.Identity.Internal.Persistence;
using Hpn.SharedKernel.Accounts;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Identity.Internal.AccountData;

/// <summary>
/// Identity's slice of account export + erasure (§10.5): the canonical user row,
/// its sessions and any outstanding sign-in tokens. This is the account's root,
/// so the orchestrator runs it last on erase (after dependent modules have been
/// purged), §10.5.
/// </summary>
internal sealed class IdentityDataContributor(IdentityDbContext dbContext) : IAccountDataContributor
{
    public string Section => "account";

    // The user row is the account's root; everything else references it by id, so
    // it is erased last (§10.5).
    public bool IsAccountRoot => true;

    public async Task<object?> ExportAsync(AccountScope scope, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == scope.UserId)
            .Select(u => new
            {
                u.Id,
                u.Email,
                Role = u.Role.ToString().ToLowerInvariant(),
                Status = u.Status.ToString().ToLowerInvariant(),
                u.CreatedAt,
                u.LastLoginAt,
                u.DeletionRequestedAt,
            })
            .FirstOrDefaultAsync(cancellationToken);

        return user;
    }

    public async Task EraseAsync(AccountScope scope, CancellationToken cancellationToken = default)
    {
        await dbContext.Sessions
            .Where(s => s.UserId == scope.UserId)
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.MagicLinkTokens
            .Where(t => t.UserId == scope.UserId)
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.Users
            .Where(u => u.Id == scope.UserId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
