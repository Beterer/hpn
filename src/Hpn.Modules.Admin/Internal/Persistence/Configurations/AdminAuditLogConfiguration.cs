using Hpn.Modules.Admin.Internal.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hpn.Modules.Admin.Internal.Persistence.Configurations;

internal sealed class AdminAuditLogConfiguration : IEntityTypeConfiguration<AdminAuditLog>
{
    public void Configure(EntityTypeBuilder<AdminAuditLog> builder)
    {
        builder.ToTable("admin_audit_log");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.AdminUserId).IsRequired();
        builder.Property(a => a.Action).HasMaxLength(120).IsRequired();
        builder.Property(a => a.TargetRef).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Metadata).HasColumnType("jsonb").IsRequired();
        builder.Property(a => a.CreatedAt).IsRequired();

        builder.HasIndex(a => new { a.AdminUserId, a.CreatedAt });
        builder.HasIndex(a => new { a.TargetRef, a.CreatedAt });
    }
}
