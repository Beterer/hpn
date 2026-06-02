using Hpn.Modules.Profile.Internal.Persistence;
using Hpn.SharedKernel.Accounts;
using Hpn.SharedKernel.Events;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Profile.Internal.AccountData;

/// <summary>
/// On soft-delete the profile must leave the feed immediately, well before the
/// grace-window purge. Marking it <c>deleted</c> takes it out of every eligibility
/// query (which requires <c>active</c>), §6.5/§10.5.
/// </summary>
internal sealed class ProfileAccountDeletionHandler(
    ProfileDbContext dbContext,
    TimeProvider timeProvider)
    : IDomainEventHandler<AccountDeletionRequested>
{
    public async Task HandleAsync(AccountDeletionRequested domainEvent, CancellationToken cancellationToken = default)
    {
        var profile = await dbContext.Profiles
            .FirstOrDefaultAsync(p => p.UserId == domainEvent.UserId, cancellationToken);
        if (profile is null)
        {
            return;
        }

        profile.MarkDeleted(timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
