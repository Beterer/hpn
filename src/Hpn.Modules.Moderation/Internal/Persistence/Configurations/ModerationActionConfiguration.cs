using Hpn.Modules.Moderation.Internal.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hpn.Modules.Moderation.Internal.Persistence.Configurations;

internal sealed class ModerationActionConfiguration : IEntityTypeConfiguration<ModerationAction>
{
    public void Configure(EntityTypeBuilder<ModerationAction> builder)
    {
        builder.ToTable("moderation_actions");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.TargetUserId).IsRequired();

        builder.Property(a => a.Action)
            .HasConversion(v => v.ToStorageValue(), v => ModerationFormat.ParseActionType(v))
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(a => a.Reason).HasMaxLength(500).IsRequired();
        builder.Property(a => a.Actor).HasMaxLength(64).IsRequired();
        builder.Property(a => a.ExpiresAt);
        builder.Property(a => a.CreatedAt).IsRequired();

        // The current state of an account is its latest action; restriction-expiry
        // and "is restricted" reads scan a target's actions newest-first (§10.3).
        builder.HasIndex(a => new { a.TargetUserId, a.CreatedAt });
    }
}
