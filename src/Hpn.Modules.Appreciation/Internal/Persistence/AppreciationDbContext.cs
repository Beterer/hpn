using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Appreciation.Internal.Persistence;

/// <summary>
/// Owns the <c>appreciation</c> schema (backbone §5.5). One DbContext per module;
/// no module ever touches another's context. Entities and configurations are
/// added per milestone.
/// </summary>
internal sealed class AppreciationDbContext(DbContextOptions<AppreciationDbContext> options) : DbContext(options)
{
    public const string Schema = "appreciation";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        base.OnModelCreating(modelBuilder);
    }
}
