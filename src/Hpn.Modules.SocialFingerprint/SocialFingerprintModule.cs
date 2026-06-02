using Hpn.Modules.SocialFingerprint.Internal;
using Hpn.Modules.SocialFingerprint.Internal.Features.GetMyFingerprint;
using Hpn.Modules.SocialFingerprint.Internal.Persistence;
using Hpn.SharedKernel.Modules;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        services.AddScoped<GetMyFingerprintHandler>();
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }

    public static IEndpointRouteBuilder MapSocialFingerprintEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGetMyFingerprint();
        return endpoints;
    }
}
