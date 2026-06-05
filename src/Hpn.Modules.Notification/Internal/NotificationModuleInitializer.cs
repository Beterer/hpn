using Hpn.Modules.Notification.Internal.Persistence;
using Hpn.SharedKernel.Modules;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Notification.Internal;

internal sealed class NotificationModuleInitializer(NotificationDbContext dbContext) : IModuleInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (dbContext.Database.GetMigrations().Any())
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
    }
}
