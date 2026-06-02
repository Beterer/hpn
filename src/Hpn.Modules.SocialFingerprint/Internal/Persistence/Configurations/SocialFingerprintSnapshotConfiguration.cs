using Hpn.Modules.SocialFingerprint.Internal.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hpn.Modules.SocialFingerprint.Internal.Persistence.Configurations;

internal sealed class SocialFingerprintSnapshotConfiguration : IEntityTypeConfiguration<SocialFingerprintSnapshot>
{
    public void Configure(EntityTypeBuilder<SocialFingerprintSnapshot> builder)
    {
        builder.ToTable("social_fingerprint_snapshots");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.ProfileId).IsRequired();
        builder.Property(s => s.Period).HasMaxLength(16).IsRequired();
        builder.Property(s => s.PeriodStart).HasColumnType("date").IsRequired();
        builder.Property(s => s.SampleSize).IsRequired();
        builder.Property(s => s.Distribution).HasColumnType("jsonb").IsRequired();
        builder.Property(s => s.TopTraits).HasColumnType("jsonb").IsRequired();
        builder.Property(s => s.CreatedAt).IsRequired();

        builder.HasIndex(s => new { s.ProfileId, s.Period, s.PeriodStart }).IsUnique();
        builder.HasIndex(s => new { s.ProfileId, s.PeriodStart });
    }
}
