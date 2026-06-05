using Hpn.Modules.Notification.Internal.Persistence;
using Hpn.SharedKernel.Auth;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Notification.Internal.Features.MarkNotificationsSeen;

internal sealed class MarkNotificationsSeenHandler(
    NotificationDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
{
    public async Task HandleAsync(CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        var now = timeProvider.GetUtcNow();

        await dbContext.Notifications
            .Where(n => n.UserId == userId && n.SeenAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.SeenAt, now), cancellationToken);
    }
}
