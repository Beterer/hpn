using Hpn.Modules.Moderation.Internal.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hpn.Modules.Moderation.Internal.Persistence.Configurations;

internal sealed class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToTable("reports");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.ReporterUserId).IsRequired();
        builder.Property(r => r.TargetProfileId).IsRequired();

        builder.Property(r => r.Type)
            .HasConversion(v => v.ToStorageValue(), v => ModerationFormat.ParseReportType(v))
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(r => r.Note).HasMaxLength(1000);

        builder.Property(r => r.Status)
            .HasConversion(v => v.ToStorageValue(), v => ModerationFormat.ParseReportStatus(v))
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.ResolvedAt);

        // Review-queue + pressure reads filter by target (§7.8), and the pressure
        // window scans a target's recent reports.
        builder.HasIndex(r => new { r.TargetProfileId, r.Status });
        builder.HasIndex(r => new { r.TargetProfileId, r.CreatedAt });

        // Duplicate reports collapse (§10.3): one report per (reporter, target, type).
        builder.HasIndex(r => new { r.ReporterUserId, r.TargetProfileId, r.Type }).IsUnique();
    }
}
