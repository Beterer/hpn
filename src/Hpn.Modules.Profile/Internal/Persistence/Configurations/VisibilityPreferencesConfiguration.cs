using Hpn.Modules.Profile.Internal.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hpn.Modules.Profile.Internal.Persistence.Configurations;

internal sealed class VisibilityPreferencesConfiguration : IEntityTypeConfiguration<VisibilityPreferences>
{
    public void Configure(EntityTypeBuilder<VisibilityPreferences> builder)
    {
        builder.ToTable("visibility_preferences");
        builder.HasKey(v => v.ProfileId);

        builder.Property(v => v.ShowOnlyOutsideCountry).HasDefaultValue(false).IsRequired();
        builder.Property(v => v.HideFromCountry).HasDefaultValue(false).IsRequired();
        builder.Property(v => v.WomenForWomen).HasDefaultValue(false).IsRequired();
        builder.Property(v => v.VerifiedOnly).HasDefaultValue(false).IsRequired();
        builder.Property(v => v.Paused).HasDefaultValue(false).IsRequired();
    }
}
