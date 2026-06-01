using Hpn.Modules.Appreciation.Contracts.Events;
using Hpn.Modules.Appreciation.Internal.Persistence;
using Hpn.SharedKernel.Events;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Appreciation.Internal;

/// <summary>
/// Inline projection updates for the core loop. The submit handler dispatches
/// <see cref="AppreciationCreated"/> while its transaction is open, so these
/// upserts commit atomically with the event row.
/// </summary>
internal sealed class AppreciationCounterProjectionHandler(AppreciationDbContext dbContext)
    : IDomainEventHandler<AppreciationCreated>
{
    public async Task HandleAsync(AppreciationCreated domainEvent, CancellationToken cancellationToken = default)
    {
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO appreciation.received_appreciation_stats
                 (receiver_profile_id, category_id, count, last_at)
             VALUES
                 ({domainEvent.ReceiverProfileId}, {domainEvent.CategoryId}, 1, {domainEvent.OccurredAt})
             ON CONFLICT (receiver_profile_id, category_id)
             DO UPDATE SET
                 count = appreciation.received_appreciation_stats.count + 1,
                 last_at = EXCLUDED.last_at
             """,
            cancellationToken);

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO appreciation.given_appreciation_stats
                 (sender_user_id, category_id, count)
             VALUES
                 ({domainEvent.SenderUserId}, {domainEvent.CategoryId}, 1)
             ON CONFLICT (sender_user_id, category_id)
             DO UPDATE SET
                 count = appreciation.given_appreciation_stats.count + 1
             """,
            cancellationToken);
    }
}
