using Hpn.Modules.Moderation.Internal.Domain;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Moderation.Internal.Persistence;

/// <summary>
/// Owns the <c>moderation</c> schema (backbone §5.5). One DbContext per module;
/// no module ever touches another's context.
/// </summary>
internal sealed class ModerationDbContext(DbContextOptions<ModerationDbContext> options) : DbContext(options)
{
    public const string Schema = "moderation";

    public DbSet<Report> Reports => Set<Report>();
    public DbSet<ModerationAction> ModerationActions => Set<ModerationAction>();
    public DbSet<AccountTrust> AccountTrust => Set<AccountTrust>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ModerationDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
