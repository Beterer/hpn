using Hpn.SharedKernel.Development;
using Microsoft.Extensions.Options;

namespace Hpn.Api.Infrastructure;

internal static class DevelopmentSeedExtensions
{
    public static IServiceCollection AddDevelopmentSeed(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DevelopmentSeedOptions>(configuration.GetSection(DevelopmentSeedOptions.SectionName));
        return services;
    }

    public static async Task SeedDevelopmentDataAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<DevelopmentSeedOptions>>().Value;
        if (!options.Enabled)
        {
            return;
        }

        var context = new DevelopmentSeedContext(options);
        var seeders = scope.ServiceProvider
            .GetServices<IDevelopmentDataSeeder>()
            .OrderBy(seeder => seeder.Phase)
            .ThenBy(seeder => seeder.GetType().FullName, StringComparer.Ordinal)
            .ToArray();

        foreach (var seeder in seeders)
        {
            await seeder.SeedAsync(context);
        }
    }
}
