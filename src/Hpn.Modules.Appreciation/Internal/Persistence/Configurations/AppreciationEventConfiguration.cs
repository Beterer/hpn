using Hpn.Modules.Appreciation.Internal.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hpn.Modules.Appreciation.Internal.Persistence.Configurations;

internal sealed class AppreciationEventConfiguration : IEntityTypeConfiguration<AppreciationEvent>
{
    public void Configure(EntityTypeBuilder<AppreciationEvent> builder)
    {
        builder.ToTable("appreciation_events");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.SenderUserId).IsRequired();
        builder.Property(e => e.ReceiverProfileId).IsRequired();
        builder.Property(e => e.CategoryId).IsRequired();
        builder.Property(e => e.TraitId).IsRequired();
        builder.Property(e => e.IdempotencyKey).HasMaxLength(128).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();

        // Duplicate-spam guard (ADR-025): a sender may appreciate a given receiver in
        // each category at most once. Stays at the category level even though the
        // event now carries a trait, which is sufficient because the feed is
        // appreciate-once-then-advance and never re-shows a profile.
        builder.HasIndex(e => new { e.SenderUserId, e.ReceiverProfileId, e.CategoryId }).IsUnique();
        builder.HasIndex(e => new { e.SenderUserId, e.IdempotencyKey }).IsUnique();
        // Receiver-scoped reads: the Feed anti-join (receiver equality) and the
        // received view's recent-events query, which filters by receiver and sorts
        // newest-first. The composite (receiver, created_at desc) serves the bare
        // receiver lookup as a prefix and lets the recent-events page avoid a sort.
        builder.HasIndex(e => new { e.ReceiverProfileId, e.CreatedAt }).IsDescending(false, true);
        builder.HasIndex(e => new { e.SenderUserId, e.CreatedAt });

        builder.HasOne<AppreciationCategory>()
            .WithMany()
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AppreciationTrait>()
            .WithMany()
            .HasForeignKey(e => e.TraitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
