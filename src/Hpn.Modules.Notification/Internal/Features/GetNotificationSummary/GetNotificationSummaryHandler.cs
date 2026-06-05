using Hpn.Modules.Notification.Internal.Domain;
using Hpn.Modules.Notification.Internal.Persistence;
using Hpn.SharedKernel.Auth;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Notification.Internal.Features.GetNotificationSummary;

internal sealed class GetNotificationSummaryHandler(NotificationDbContext dbContext, ICurrentUser currentUser)
{
    public async Task<GetNotificationSummaryResponse> HandleAsync(CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();

        var unseenCount = await dbContext.Notifications
            .AsNoTracking()
            .CountAsync(n => n.UserId == userId && n.SeenAt == null, cancellationToken);

        var rows = await dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new { n.Id, n.Type, n.TraitLabel, n.CategorySlug, n.CreatedAt, Seen = n.SeenAt != null })
            .Take(1)
            .ToArrayAsync(cancellationToken);

        var latest = rows.Length == 0
            ? null
            : new NotificationItemResponse(
                rows[0].Id,
                NotificationTypeFormat.ToStorageValue(rows[0].Type),
                rows[0].TraitLabel,
                rows[0].CategorySlug,
                rows[0].CreatedAt,
                rows[0].Seen);

        return new GetNotificationSummaryResponse(unseenCount, latest);
    }
}
