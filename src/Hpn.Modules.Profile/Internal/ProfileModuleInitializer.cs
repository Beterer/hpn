using Hpn.Modules.Profile.Internal.Persistence;
using Hpn.SharedKernel.Modules;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Profile.Internal;

/// <summary>
/// Applies the Profile module's migrations on startup. A no-op until the module
/// declares its first migration (M0 schemas are empty by design).
/// </summary>
internal sealed class ProfileModuleInitializer(ProfileDbContext dbContext) : IModuleInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (dbContext.Database.GetMigrations().Any())
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
    }
}
