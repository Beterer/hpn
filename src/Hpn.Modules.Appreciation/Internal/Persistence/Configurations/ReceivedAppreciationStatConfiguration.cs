using Hpn.Modules.Appreciation.Internal.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hpn.Modules.Appreciation.Internal.Persistence.Configurations;

internal sealed class ReceivedAppreciationStatConfiguration : IEntityTypeConfiguration<ReceivedAppreciationStat>
{
    public void Configure(EntityTypeBuilder<ReceivedAppreciationStat> builder)
    {
        builder.ToTable("received_appreciation_stats");
        builder.HasKey(s => new { s.ReceiverProfileId, s.CategoryId });

        builder.Property(s => s.Count).IsRequired();
        builder.Property(s => s.LastAt).IsRequired();

        builder.HasOne<AppreciationCategory>()
            .WithMany()
            .HasForeignKey(s => s.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
