using Hpn.Modules.Admin.Internal.Domain;
using Hpn.Modules.Admin.Internal.ReadModel;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Admin.Internal.Persistence;

/// <summary>
/// Owns the <c>admin</c> schema (backbone §5.5). One DbContext per module;
/// no module ever touches another's context. Entities and configurations are
/// added per milestone.
/// </summary>
internal sealed class AdminDbContext(DbContextOptions<AdminDbContext> options) : DbContext(options)
{
    public const string Schema = "admin";

    public DbSet<AdminAuditLog> AdminAuditLog => Set<AdminAuditLog>();
    public DbSet<AdminQueueItemReadModel> QueueItems => Set<AdminQueueItemReadModel>();
    public DbSet<AdminReportReadModel> Reports => Set<AdminReportReadModel>();
    public DbSet<AdminStatsReadModel> Stats => Set<AdminStatsReadModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AdminDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
