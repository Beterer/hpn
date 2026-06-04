using Hpn.Modules.Appreciation.Internal.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hpn.Modules.Appreciation.Internal.Persistence.Configurations;

internal sealed class AppreciationTraitConfiguration : IEntityTypeConfiguration<AppreciationTrait>
{
    public void Configure(EntityTypeBuilder<AppreciationTrait> builder)
    {
        builder.ToTable("appreciation_traits");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.CategoryId).IsRequired();
        builder.Property(t => t.Slug).HasMaxLength(64).IsRequired();
        builder.Property(t => t.Label).HasMaxLength(80).IsRequired();
        builder.Property(t => t.SortOrder).IsRequired();
        builder.Property(t => t.Active).IsRequired();

        builder.HasIndex(t => t.Slug).IsUnique();
        builder.HasIndex(t => t.SortOrder).IsUnique();
        builder.HasIndex(t => t.CategoryId);

        builder.HasOne<AppreciationCategory>()
            .WithMany()
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(AppreciationTraitSeed.All);
    }
}
