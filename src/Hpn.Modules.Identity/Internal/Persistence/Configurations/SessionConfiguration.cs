using Hpn.Modules.Identity.Internal.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hpn.Modules.Identity.Internal.Persistence.Configurations;

internal sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("sessions");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.TokenHash).IsRequired();
        builder.HasIndex(s => s.TokenHash).IsUnique();

        builder.HasIndex(s => s.UserId);

        builder.Property(s => s.ExpiresAt).IsRequired();
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.UserAgent).HasMaxLength(512);
        builder.Property(s => s.Ip).HasMaxLength(64);
    }
}
