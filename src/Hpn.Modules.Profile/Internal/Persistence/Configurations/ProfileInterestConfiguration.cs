using Hpn.Modules.Profile.Internal.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hpn.Modules.Profile.Internal.Persistence.Configurations;

internal sealed class ProfileInterestConfiguration : IEntityTypeConfiguration<ProfileInterest>
{
    public void Configure(EntityTypeBuilder<ProfileInterest> builder)
    {
        builder.ToTable("profile_interests");
        builder.HasKey(pi => new { pi.ProfileId, pi.InterestId });

        builder.HasOne(pi => pi.Interest)
            .WithMany()
            .HasForeignKey(pi => pi.InterestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
