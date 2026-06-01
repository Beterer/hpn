using Hpn.Modules.Identity.Internal.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hpn.Modules.Identity.Internal.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .HasColumnType("citext")
            .IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.Role)
            .HasConversion(v => v.ToString().ToLowerInvariant(), v => Enum.Parse<UserRole>(v, ignoreCase: true))
            .IsRequired();

        builder.Property(u => u.Status)
            .HasConversion(v => v.ToString().ToLowerInvariant(), v => Enum.Parse<UserStatus>(v, ignoreCase: true))
            .IsRequired();

        builder.Property(u => u.CreatedAt).IsRequired();
    }
}
