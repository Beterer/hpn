using Hpn.Modules.Identity.Internal.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hpn.Modules.Identity.Internal.Persistence.Configurations;

internal sealed class GuestSessionConfiguration : IEntityTypeConfiguration<GuestSession>
{
    public void Configure(EntityTypeBuilder<GuestSession> builder)
    {
        builder.ToTable("guest_sessions");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.TokenHash).HasMaxLength(64).IsRequired();
        builder.Property(s => s.ExpiresAt).IsRequired();
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.UserAgent).HasMaxLength(512);
        builder.Property(s => s.Ip).HasMaxLength(128);

        builder.HasIndex(s => s.TokenHash).IsUnique();
        builder.HasIndex(s => s.ExpiresAt);
    }
}
