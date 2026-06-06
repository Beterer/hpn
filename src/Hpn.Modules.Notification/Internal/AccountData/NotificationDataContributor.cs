using Hpn.Modules.Notification.Internal.Domain;
using Hpn.Modules.Notification.Internal.Persistence;
using Hpn.SharedKernel.Accounts;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Notification.Internal.AccountData;

internal sealed class NotificationDataContributor(NotificationDbContext dbContext) : IAccountDataContributor
{
    public string Section => "notifications";

    public async Task<object?> ExportAsync(AccountScope scope, CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == scope.UserId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new { n.Type, n.TraitLabel, n.CategorySlug, n.Phrasing, n.CreatedAt, n.SeenAt })
            .ToArrayAsync(cancellationToken);

        if (rows.Length == 0)
        {
            return null;
        }

        return new
        {
            Notifications = rows.Select(n => new
            {
                Type = NotificationTypeFormat.ToStorageValue(n.Type),
                n.TraitLabel,
                n.CategorySlug,
                n.Phrasing,
                n.CreatedAt,
                n.SeenAt,
            }).ToArray(),
        };
    }

    public async Task EraseAsync(AccountScope scope, CancellationToken cancellationToken = default)
    {
        await dbContext.Notifications
            .Where(n => n.UserId == scope.UserId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
