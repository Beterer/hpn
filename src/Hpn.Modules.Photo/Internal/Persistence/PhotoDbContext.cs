using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Photo.Internal.Persistence;

/// <summary>
/// Owns the <c>photo</c> schema (backbone §5.5). One DbContext per module;
/// no module ever touches another's context. Entities and configurations are
/// added per milestone.
/// </summary>
internal sealed class PhotoDbContext(DbContextOptions<PhotoDbContext> options) : DbContext(options)
{
    public const string Schema = "photo";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        base.OnModelCreating(modelBuilder);
    }
}
