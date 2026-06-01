using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Hpn.Modules.Identity.Internal.Persistence;

/// <summary>
/// Design-time factory for <c>dotnet ef migrations</c> only. Keeps migration
/// authoring decoupled from booting the web host; the connection string here is
/// used purely for SQL generation, never to connect (backbone §13.3). It mirrors
/// the runtime provider + naming convention so generated SQL matches production.
/// </summary>
internal sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=localhost;Port=55432;Database=hpn;Username=hpn;Password=hpn";

        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new IdentityDbContext(options);
    }
}
