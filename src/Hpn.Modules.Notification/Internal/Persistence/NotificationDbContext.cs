using Microsoft.EntityFrameworkCore;
using NotificationEntity = Hpn.Modules.Notification.Internal.Domain.Notification;

namespace Hpn.Modules.Notification.Internal.Persistence;

/// <summary>Owns the <c>notification</c> schema.</summary>
internal sealed class NotificationDbContext(DbContextOptions<NotificationDbContext> options) : DbContext(options)
{
    public const string Schema = "notification";

    public DbSet<NotificationEntity> Notifications => Set<NotificationEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
