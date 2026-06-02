using Hpn.Modules.Profile.Internal.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hpn.Modules.Profile.Internal.Persistence.Configurations;

internal sealed class ProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("profiles");
        builder.HasKey(p => p.Id);

        builder.HasIndex(p => p.UserId).IsUnique();

        builder.Property(p => p.DisplayName)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(p => p.Gender)
            .HasConversion(v => v.ToStorageValue(), v => ProfileFormat.ParseGender(v))
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(p => p.SelfDescribeText)
            .HasMaxLength(80);

        builder.Property(p => p.CountryCode)
            .HasMaxLength(2)
            .IsFixedLength();

        builder.Property(p => p.Bio)
            .HasMaxLength(500);

        // Coarse geopoint (§10.4) — stored as two rounded columns, null until consent.
        builder.Property(p => p.GeoLat);
        builder.Property(p => p.GeoLng);
        builder.Property(p => p.LocationConsent)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(p => p.Verified)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(p => p.Status)
            .HasConversion(v => v.ToStorageValue(), v => ProfileFormat.ParseStatus(v))
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();

        builder.HasMany(p => p.ProfileInterests)
            .WithOne()
            .HasForeignKey(pi => pi.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.ProfileInterests)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne(p => p.VisibilityPreferences)
            .WithOne()
            .HasForeignKey<VisibilityPreferences>(v => v.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.VisibilityPreferences).AutoInclude();
    }
}
