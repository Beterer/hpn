using Hpn.Modules.Profile.Internal.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hpn.Modules.Profile.Internal.Persistence.Configurations;

internal sealed class InterestConfiguration : IEntityTypeConfiguration<Interest>
{
    public void Configure(EntityTypeBuilder<Interest> builder)
    {
        builder.ToTable("interests");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Slug)
            .HasMaxLength(64)
            .IsRequired();
        builder.HasIndex(i => i.Slug).IsUnique();

        builder.Property(i => i.Label)
            .HasMaxLength(80)
            .IsRequired();

        builder.HasData(InterestSeed.All);
    }
}
