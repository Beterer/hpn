using Hpn.Modules.Photo.Internal;
using Hpn.Modules.Photo.Internal.Persistence;
using Hpn.SharedKernel.Modules;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hpn.Modules.Photo;

/// <summary>
/// Composition root for the Photo module. The host calls these two methods and
/// nothing else reaches inside (backbone §5.3, §5.4).
/// </summary>
public static class PhotoModule
{
    public static IServiceCollection AddPhotoModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PhotoDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Postgres"))
                   .UseSnakeCaseNamingConvention());

        services.AddScoped<IModuleInitializer, PhotoModuleInitializer>();

        return services;
    }

    public static IEndpointRouteBuilder MapPhotoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Vertical-slice endpoints are mapped here per milestone (M1+).
        return endpoints;
    }
}
