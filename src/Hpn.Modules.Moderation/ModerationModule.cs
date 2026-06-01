using Hpn.Modules.Moderation.Internal;
using Hpn.Modules.Moderation.Internal.Persistence;
using Hpn.SharedKernel.Modules;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hpn.Modules.Moderation;

/// <summary>
/// Composition root for the Moderation module. The host calls these two methods and
/// nothing else reaches inside (backbone §5.3, §5.4).
/// </summary>
public static class ModerationModule
{
    public static IServiceCollection AddModerationModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ModerationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Postgres"))
                   .UseSnakeCaseNamingConvention());

        services.AddScoped<IModuleInitializer, ModerationModuleInitializer>();

        return services;
    }

    public static IEndpointRouteBuilder MapModerationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Vertical-slice endpoints are mapped here per milestone (M1+).
        return endpoints;
    }
}
