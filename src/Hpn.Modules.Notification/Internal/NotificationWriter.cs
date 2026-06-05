using Hpn.Modules.Notification.Internal.Domain;
using Hpn.Modules.Notification.Internal.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Notification.Internal;

/// <summary>
/// The single notification-creation point. A future server-push transport can
/// fan out from here without changing event handlers.
/// </summary>
internal sealed class NotificationWriter(NotificationDbContext dbContext)
{
    public async Task CreateAppreciationReceivedAsync(
        Guid userId,
        Guid sourceAppreciationId,
        string traitLabel,
        string categorySlug,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken = default)
    {
        var id = Guid.CreateVersion7();
        var type = NotificationTypeFormat.ToStorageValue(NotificationType.AppreciationReceived);

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO notification.notifications
                 (id, user_id, type, source_id, trait_label, category_slug, created_at, seen_at)
             VALUES
                 ({id}, {userId}, {type}, {sourceAppreciationId}, {traitLabel}, {categorySlug}, {occurredAt}, NULL)
             ON CONFLICT (user_id, type, source_id) DO NOTHING
             """,
            cancellationToken);
    }
}
