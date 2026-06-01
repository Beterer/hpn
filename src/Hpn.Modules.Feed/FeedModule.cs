using Hpn.Modules.Feed.Contracts;
using Hpn.Modules.Feed.Internal;
using Hpn.Modules.Feed.Internal.Features.GetNext;
using Hpn.Modules.Feed.Internal.Persistence;
using Hpn.Modules.Feed.Internal.Ranking;
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

        // The swap surface (backbone §6.5): replacing the ranking algorithm means
        // registering a different IFeedRankingStrategy here — eligibility, the
        // contract, and callers stay put.
        services.AddScoped<IFeedRankingStrategy, RandomWithinEligibleStrategy>();

        services.AddScoped<GetFeedNextHandler>();
        services.AddScoped<IFeedApi, FeedApi>();

        return services;
    }

    public static IEndpointRouteBuilder MapFeedEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGetFeedNext();

        return endpoints;
    }
}
