using Hpn.Modules.Feed.Internal;
using Hpn.Modules.Feed.Internal.Persistence;
using Hpn.SharedKernel.Modules;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hpn.Modules.Feed;

/// <summary>
/// Composition root for the Feed module. The host calls these two methods and
/// nothing else reaches inside (backbone §5.3, §5.4).
/// </summary>
public static class FeedModule
{
    public static IServiceCollection AddFeedModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<FeedDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Postgres"))
                   .UseSnakeCaseNamingConvention());

        services.AddScoped<IModuleInitializer, FeedModuleInitializer>();

        return services;
    }

    public static IEndpointRouteBuilder MapFeedEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Vertical-slice endpoints are mapped here per milestone (M1+).
        return endpoints;
    }
}
