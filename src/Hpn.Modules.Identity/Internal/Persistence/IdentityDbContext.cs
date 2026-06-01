using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Identity.Internal.Persistence;

/// <summary>
/// Owns the <c>identity</c> schema (backbone §5.5). One DbContext per module;
/// no module ever touches another's context. Entities and configurations are
/// added per milestone.
/// </summary>
internal sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    public const string Schema = "identity";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        base.OnModelCreating(modelBuilder);
    }
}
