using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Feed.Internal.Persistence;

/// <summary>
/// Owns the <c>feed</c> schema (backbone §5.5). One DbContext per module;
/// no module ever touches another's context. Entities and configurations are
/// added per milestone.
/// </summary>
internal sealed class FeedDbContext(DbContextOptions<FeedDbContext> options) : DbContext(options)
{
    public const string Schema = "feed";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        base.OnModelCreating(modelBuilder);
    }
}
