using Hpn.Modules.Appreciation.Internal.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hpn.Modules.Appreciation.Internal.Persistence.Configurations;

internal sealed class AppreciationCategoryConfiguration : IEntityTypeConfiguration<AppreciationCategory>
{
    public void Configure(EntityTypeBuilder<AppreciationCategory> builder)
    {
        builder.ToTable("appreciation_categories");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Slug).HasMaxLength(64).IsRequired();
        builder.Property(c => c.Label).HasMaxLength(80).IsRequired();
        builder.Property(c => c.SortOrder).IsRequired();
        builder.Property(c => c.Hue).IsRequired();
        builder.Property(c => c.Active).IsRequired();

        builder.HasIndex(c => c.Slug).IsUnique();
        builder.HasIndex(c => c.SortOrder).IsUnique();

        builder.HasData(AppreciationCategorySeed.All);
    }
}
