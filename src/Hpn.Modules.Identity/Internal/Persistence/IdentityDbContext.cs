using Hpn.Modules.Identity.Internal.Domain;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Identity.Internal.Persistence;

/// <summary>
/// Owns the <c>identity</c> schema (backbone §5.5). One DbContext per module;
/// no module ever touches another's context. References to other modules' rows
/// are by <c>uuid</c> only — no cross-schema FKs (§7).
/// </summary>
internal sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    public const string Schema = "identity";

    public DbSet<User> Users => Set<User>();
    public DbSet<MagicLinkToken> MagicLinkTokens => Set<MagicLinkToken>();
    public DbSet<Session> Sessions => Set<Session>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        // case-insensitive email uniqueness (backbone §7.2: email citext unique).
        modelBuilder.HasPostgresExtension("citext");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
