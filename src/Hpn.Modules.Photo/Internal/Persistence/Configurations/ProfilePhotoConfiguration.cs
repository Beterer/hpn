using Hpn.Modules.Photo.Internal.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hpn.Modules.Photo.Internal.Persistence.Configurations;

internal sealed class ProfilePhotoConfiguration : IEntityTypeConfiguration<ProfilePhoto>
{
    public void Configure(EntityTypeBuilder<ProfilePhoto> builder)
    {
        builder.ToTable("photos");
        builder.HasKey(p => p.Id);

        builder.HasIndex(p => new { p.ProfileId, p.Position }).IsUnique();
        builder.HasIndex(p => p.ContentHash);

        builder.Property(p => p.Status)
            .HasConversion(v => v.ToStorageValue(), v => PhotoFormat.ParseStatus(v))
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(p => p.OriginalKey)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(p => p.DisplayKey)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(p => p.ThumbKey)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(p => p.ContentHash)
            .HasMaxLength(64)
            .IsFixedLength()
            .IsRequired();

        builder.Property(p => p.ScanResult)
            .HasMaxLength(64);

        builder.Property(p => p.CreatedAt).IsRequired();
    }
}
