using Hpn.Modules.Moderation.Internal.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hpn.Modules.Moderation.Internal.Persistence.Configurations;

internal sealed class AccountTrustConfiguration : IEntityTypeConfiguration<AccountTrust>
{
    public void Configure(EntityTypeBuilder<AccountTrust> builder)
    {
        builder.ToTable("account_trust");
        builder.HasKey(t => t.UserId);

        builder.Property(t => t.UserId).ValueGeneratedNever();
        builder.Property(t => t.Score).HasColumnType("numeric(4,3)").IsRequired();
        builder.Property(t => t.Signals).HasColumnType("jsonb").IsRequired();
        builder.Property(t => t.UpdatedAt).IsRequired();
    }
}
