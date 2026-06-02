using Hpn.Modules.SocialFingerprint.Internal.Domain;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.SocialFingerprint.Internal.Persistence;

/// <summary>
/// Owns the <c>social_fingerprint</c> schema (backbone §5.5). One DbContext per module;
/// no module ever touches another's context. Entities and configurations are
/// added per milestone.
/// </summary>
internal sealed class SocialFingerprintDbContext(DbContextOptions<SocialFingerprintDbContext> options) : DbContext(options)
{
    public const string Schema = "social_fingerprint";

    public DbSet<SocialFingerprintSnapshot> SocialFingerprintSnapshots => Set<SocialFingerprintSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SocialFingerprintDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
