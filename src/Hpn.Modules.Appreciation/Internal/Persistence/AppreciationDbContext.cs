using Hpn.Modules.Appreciation.Internal.Domain;
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

    public DbSet<AppreciationCategory> AppreciationCategories => Set<AppreciationCategory>();
    public DbSet<AppreciationEvent> AppreciationEvents => Set<AppreciationEvent>();
    public DbSet<ReceivedAppreciationStat> ReceivedAppreciationStats => Set<ReceivedAppreciationStat>();
    public DbSet<GivenAppreciationStat> GivenAppreciationStats => Set<GivenAppreciationStat>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppreciationDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
