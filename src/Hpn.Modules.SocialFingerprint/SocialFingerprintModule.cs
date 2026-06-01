using Hpn.Modules.SocialFingerprint.Internal;
using Hpn.Modules.SocialFingerprint.Internal.Persistence;
using Hpn.SharedKernel.Modules;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hpn.Modules.SocialFingerprint;

/// <summary>
/// Composition root for the SocialFingerprint module. The host calls these two methods and
/// nothing else reaches inside (backbone §5.3, §5.4).
/// </summary>
public static class SocialFingerprintModule
{
    public static IServiceCollection AddSocialFingerprintModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<SocialFingerprintDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Postgres"))
                   .UseSnakeCaseNamingConvention());

        services.AddScoped<IModuleInitializer, SocialFingerprintModuleInitializer>();

        return services;
    }

    public static IEndpointRouteBuilder MapSocialFingerprintEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Vertical-slice endpoints are mapped here per milestone (M1+).
        return endpoints;
    }
}
