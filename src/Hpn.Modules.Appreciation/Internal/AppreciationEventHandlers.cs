using Hpn.Modules.Appreciation.Contracts.Events;
using Hpn.Modules.Appreciation.Internal.Persistence;
using Hpn.Modules.Profile.Contracts;
using Hpn.SharedKernel.Accounts;
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

internal sealed class GuestConvertedAppreciationHandler(AppreciationDbContext dbContext, IProfileApi profileApi)
    : IDomainEventHandler<GuestConverted>
{
    public async Task HandleAsync(GuestConverted domainEvent, CancellationToken cancellationToken = default)
    {
        // The guest feed has no self-exclusion (a guest has no profile), so a logged-out
        // member can appreciate their own profile while browsing. Such events must not be
        // re-keyed onto the converting user — that would be a self-appreciation the submit
        // path forbids. Drop them alongside the member-collision events. Null (no profile
        // yet) makes the predicate simply false.
        var selfProfileId = await profileApi.GetProfileIdForUserAsync(domainEvent.UserId, cancellationToken);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        // Guest events that would collide with existing member events (or appreciate the
        // converting user's own profile) cannot be re-keyed. Remove them and compensate
        // receiver counters so the read models still reflect the surviving event rows.
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             WITH deleted AS (
                 DELETE FROM appreciation.appreciation_events AS guest_event
                 WHERE guest_event.sender_user_id = {domainEvent.GuestId}
                   AND (
                       guest_event.receiver_profile_id = {selfProfileId}
                       OR EXISTS (
                           SELECT 1
                           FROM appreciation.appreciation_events AS member_event
                           WHERE member_event.sender_user_id = {domainEvent.UserId}
                             AND (
                                 (member_event.receiver_profile_id = guest_event.receiver_profile_id
                                  AND member_event.category_id = guest_event.category_id)
                                 OR member_event.idempotency_key = guest_event.idempotency_key
                             )
                       )
                   )
                 RETURNING guest_event.receiver_profile_id, guest_event.category_id
             ),
             decremented AS (
                 UPDATE appreciation.received_appreciation_stats AS stats
                 SET count = GREATEST(0, stats.count - deleted_counts.count)
                 FROM (
                     SELECT receiver_profile_id, category_id, COUNT(*)::int AS count
                     FROM deleted
                     GROUP BY receiver_profile_id, category_id
                 ) AS deleted_counts
                 WHERE stats.receiver_profile_id = deleted_counts.receiver_profile_id
                   AND stats.category_id = deleted_counts.category_id
                 RETURNING stats.receiver_profile_id, stats.category_id, stats.count
             )
             DELETE FROM appreciation.received_appreciation_stats AS stats
             USING decremented
             WHERE stats.receiver_profile_id = decremented.receiver_profile_id
               AND stats.category_id = decremented.category_id
               AND decremented.count = 0
             """,
            cancellationToken);

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             UPDATE appreciation.appreciation_events
             SET sender_user_id = {domainEvent.UserId}
             WHERE sender_user_id = {domainEvent.GuestId}
             """,
            cancellationToken);

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO appreciation.given_appreciation_stats
                 (sender_user_id, category_id, count)
             SELECT {domainEvent.UserId}, category_id, count
             FROM appreciation.given_appreciation_stats
             WHERE sender_user_id = {domainEvent.GuestId}
             ON CONFLICT (sender_user_id, category_id)
             DO UPDATE SET
                 count = appreciation.given_appreciation_stats.count + EXCLUDED.count
             """,
            cancellationToken);

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             DELETE FROM appreciation.given_appreciation_stats
             WHERE sender_user_id = {domainEvent.GuestId}
             """,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
