using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Hpn.Modules.Photo.Internal.Persistence;

/// <summary>
/// Design-time factory for <c>dotnet ef migrations</c>. Mirrors runtime provider
/// + snake_case naming so generated SQL matches the module schema.
/// </summary>
internal sealed class PhotoDbContextFactory : IDesignTimeDbContextFactory<PhotoDbContext>
{
    public PhotoDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=localhost;Port=55432;Database=hpn;Username=hpn;Password=hpn";

        var options = new DbContextOptionsBuilder<PhotoDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new PhotoDbContext(options);
    }
}
