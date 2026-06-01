using Hpn.Modules.Appreciation.Internal;
using Hpn.Modules.Appreciation.Internal.Persistence;
using Hpn.SharedKernel.Modules;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hpn.Modules.Appreciation;

/// <summary>
/// Composition root for the Appreciation module. The host calls these two methods and
/// nothing else reaches inside (backbone §5.3, §5.4).
/// </summary>
public static class AppreciationModule
{
    public static IServiceCollection AddAppreciationModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppreciationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Postgres"))
                   .UseSnakeCaseNamingConvention());

        services.AddScoped<IModuleInitializer, AppreciationModuleInitializer>();

        return services;
    }

    public static IEndpointRouteBuilder MapAppreciationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Vertical-slice endpoints are mapped here per milestone (M1+).
        return endpoints;
    }
}
