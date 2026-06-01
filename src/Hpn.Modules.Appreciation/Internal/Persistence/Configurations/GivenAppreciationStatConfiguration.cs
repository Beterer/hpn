using Hpn.Modules.Appreciation.Internal.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hpn.Modules.Appreciation.Internal.Persistence.Configurations;

internal sealed class GivenAppreciationStatConfiguration : IEntityTypeConfiguration<GivenAppreciationStat>
{
    public void Configure(EntityTypeBuilder<GivenAppreciationStat> builder)
    {
        builder.ToTable("given_appreciation_stats");
        builder.HasKey(s => new { s.SenderUserId, s.CategoryId });

        builder.Property(s => s.Count).IsRequired();

        builder.HasOne<AppreciationCategory>()
            .WithMany()
            .HasForeignKey(s => s.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
