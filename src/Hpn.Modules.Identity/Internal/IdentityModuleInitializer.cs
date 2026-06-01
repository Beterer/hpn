using Hpn.Modules.Identity.Internal.Persistence;
using Hpn.SharedKernel.Modules;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Identity.Internal;

/// <summary>
/// Applies the Identity module's migrations on startup. A no-op until the module
/// declares its first migration (M0 schemas are empty by design).
/// </summary>
internal sealed class IdentityModuleInitializer(IdentityDbContext dbContext) : IModuleInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (dbContext.Database.GetMigrations().Any())
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
    }
}
