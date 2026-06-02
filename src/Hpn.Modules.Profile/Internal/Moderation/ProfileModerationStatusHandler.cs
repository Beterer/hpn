using Hpn.Modules.Profile.Internal.Persistence;
using Hpn.SharedKernel.Events;
using Hpn.SharedKernel.Moderation;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Profile.Internal.Moderation;

/// <summary>
/// Reflects Moderation's account-state decisions into the profile lifecycle so the
/// feed honours them with no extra coupling: eligibility already requires
/// <c>active</c> (§6.5), so moving a restricted or banned account out of that state
/// is all it takes to drop it from everyone's feed. Subscribes to the shared
/// moderation events rather than reaching into the Moderation module (§3.3) — the
/// same shape as <see cref="AccountData.ProfileAccountDeletionHandler"/>.
/// </summary>
internal sealed class ProfileModerationStatusHandler(
    ProfileDbContext dbContext,
    TimeProvider timeProvider)
    : IDomainEventHandler<UserRestricted>,
      IDomainEventHandler<UserBanned>,
      IDomainEventHandler<UserCleared>
{
    public Task HandleAsync(UserRestricted domainEvent, CancellationToken cancellationToken = default) =>
        ApplyAsync(domainEvent.UserId, p => p.Restrict(timeProvider.GetUtcNow()), cancellationToken);

    public Task HandleAsync(UserBanned domainEvent, CancellationToken cancellationToken = default) =>
        ApplyAsync(domainEvent.UserId, p => p.Ban(timeProvider.GetUtcNow()), cancellationToken);

    public Task HandleAsync(UserCleared domainEvent, CancellationToken cancellationToken = default) =>
        ApplyAsync(domainEvent.UserId, p => p.ClearModeration(timeProvider.GetUtcNow()), cancellationToken);

    private async Task ApplyAsync(Guid userId, Func<Domain.UserProfile, bool> transition, CancellationToken cancellationToken)
    {
        var profile = await dbContext.Profiles
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (profile is null)
        {
            return;
        }

        if (transition(profile))
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
