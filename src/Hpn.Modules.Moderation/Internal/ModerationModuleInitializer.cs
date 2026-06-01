using Hpn.Modules.Moderation.Internal.Persistence;
using Hpn.SharedKernel.Modules;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Moderation.Internal;

/// <summary>
/// Applies the Moderation module's migrations on startup. A no-op until the module
/// declares its first migration (M0 schemas are empty by design).
/// </summary>
internal sealed class ModerationModuleInitializer(ModerationDbContext dbContext) : IModuleInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (dbContext.Database.GetMigrations().Any())
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
    }
}
