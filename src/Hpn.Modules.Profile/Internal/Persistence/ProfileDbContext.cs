using Microsoft.EntityFrameworkCore;
using Hpn.Modules.Profile.Internal.Domain;

namespace Hpn.Modules.Profile.Internal.Persistence;

/// <summary>
/// Owns the <c>profile</c> schema (backbone §5.5). One DbContext per module;
/// no module ever touches another's context. Entities and configurations are
/// added per milestone.
/// </summary>
internal sealed class ProfileDbContext(DbContextOptions<ProfileDbContext> options) : DbContext(options)
{
    public const string Schema = "profile";

    public DbSet<UserProfile> Profiles => Set<UserProfile>();
    public DbSet<Interest> Interests => Set<Interest>();
    public DbSet<ProfileInterest> ProfileInterests => Set<ProfileInterest>();
    public DbSet<VisibilityPreferences> VisibilityPreferences => Set<VisibilityPreferences>();
    public DbSet<UserBlock> UserBlocks => Set<UserBlock>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProfileDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
