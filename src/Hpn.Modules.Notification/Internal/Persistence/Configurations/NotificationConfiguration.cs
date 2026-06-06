using Hpn.Modules.Notification.Internal.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationEntity = Hpn.Modules.Notification.Internal.Domain.Notification;

namespace Hpn.Modules.Notification.Internal.Persistence.Configurations;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<NotificationEntity>
{
    public void Configure(EntityTypeBuilder<NotificationEntity> builder)
    {
        builder.ToTable("notifications");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.UserId).IsRequired();
        builder.Property(n => n.Type)
            .HasConversion(t => NotificationTypeFormat.ToStorageValue(t), v => NotificationTypeFormat.Parse(v))
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(n => n.SourceId).IsRequired();
        builder.Property(n => n.TraitLabel).HasMaxLength(120).IsRequired();
        builder.Property(n => n.CategorySlug).HasMaxLength(64).IsRequired();
        builder.Property(n => n.Phrasing).HasMaxLength(200).IsRequired();
        builder.Property(n => n.CreatedAt).IsRequired();

        builder.HasIndex(n => new { n.UserId, n.Type, n.SourceId }).IsUnique();
        builder.HasIndex(n => new { n.UserId, n.SeenAt });
        builder.HasIndex(n => new { n.UserId, n.CreatedAt }).IsDescending(false, true);
    }
}
