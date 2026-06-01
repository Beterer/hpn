using Hpn.Modules.Profile.Internal.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hpn.Modules.Profile.Internal.Persistence.Configurations;

internal sealed class UserBlockConfiguration : IEntityTypeConfiguration<UserBlock>
{
    public void Configure(EntityTypeBuilder<UserBlock> builder)
    {
        builder.ToTable("user_blocks");
        builder.HasKey(b => new { b.BlockerUserId, b.BlockedUserId });

        builder.Property(b => b.CreatedAt).IsRequired();
    }
}
