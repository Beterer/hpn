using Hpn.Modules.Feed.Internal.ReadModel;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Feed.Internal.Persistence;

/// <summary>
/// The Feed read model (backbone §3.1, §6.5, §7.6). Feed owns no tables of its
/// own; this context maps read-only row types onto the tables that Profile,
/// Photo, and Appreciation own and migrate, so the eligibility query is a single
/// efficient cross-schema SELECT rather than a fan-out of contract calls. Every
/// mapping is <see cref="RelationalEntityTypeBuilderExtensions.ToTable(...)"/>
/// excluded from migrations — Feed reads, it never writes or owns these tables.
/// </summary>
internal sealed class FeedDbContext(DbContextOptions<FeedDbContext> options) : DbContext(options)
{
    public const string Schema = "feed";

    public DbSet<FeedProfileRow> Profiles => Set<FeedProfileRow>();
    public DbSet<FeedVisibilityRow> VisibilityPreferences => Set<FeedVisibilityRow>();
    public DbSet<FeedBlockRow> UserBlocks => Set<FeedBlockRow>();
    public DbSet<FeedPhotoRow> Photos => Set<FeedPhotoRow>();
    public DbSet<FeedAppreciationRow> AppreciationEvents => Set<FeedAppreciationRow>();
    public DbSet<FeedProfileInterestRow> ProfileInterests => Set<FeedProfileInterestRow>();
    public DbSet<FeedInterestRow> Interests => Set<FeedInterestRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<FeedProfileRow>(b =>
        {
            b.ToTable("profiles", "profile", t => t.ExcludeFromMigrations());
            b.HasKey(x => x.Id);
        });

        modelBuilder.Entity<FeedVisibilityRow>(b =>
        {
            b.ToTable("visibility_preferences", "profile", t => t.ExcludeFromMigrations());
            b.HasKey(x => x.ProfileId);
        });

        modelBuilder.Entity<FeedBlockRow>(b =>
        {
            b.ToTable("user_blocks", "profile", t => t.ExcludeFromMigrations());
            b.HasKey(x => new { x.BlockerUserId, x.BlockedUserId });
        });

        modelBuilder.Entity<FeedPhotoRow>(b =>
        {
            b.ToTable("photos", "photo", t => t.ExcludeFromMigrations());
            b.HasKey(x => x.Id);
        });

        modelBuilder.Entity<FeedAppreciationRow>(b =>
        {
            b.ToTable("appreciation_events", "appreciation", t => t.ExcludeFromMigrations());
            b.HasKey(x => x.Id);
        });

        modelBuilder.Entity<FeedProfileInterestRow>(b =>
        {
            b.ToTable("profile_interests", "profile", t => t.ExcludeFromMigrations());
            b.HasKey(x => new { x.ProfileId, x.InterestId });
        });

        modelBuilder.Entity<FeedInterestRow>(b =>
        {
            b.ToTable("interests", "profile", t => t.ExcludeFromMigrations());
            b.HasKey(x => x.Id);
        });

        base.OnModelCreating(modelBuilder);
    }
}
