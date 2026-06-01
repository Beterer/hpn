using Microsoft.EntityFrameworkCore;
using Hpn.Modules.Photo.Internal.Domain;

namespace Hpn.Modules.Photo.Internal.Persistence;

/// <summary>
/// Owns the <c>photo</c> schema (backbone §5.5). One DbContext per module;
/// no module ever touches another's context. Entities and configurations are
/// added per milestone.
/// </summary>
internal sealed class PhotoDbContext(DbContextOptions<PhotoDbContext> options) : DbContext(options)
{
    public const string Schema = "photo";

    public DbSet<ProfilePhoto> Photos => Set<ProfilePhoto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PhotoDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
