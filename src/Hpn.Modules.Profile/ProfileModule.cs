using FluentValidation;
using Hpn.Modules.Profile.Contracts;
using Hpn.Modules.Profile.Internal;
using Hpn.Modules.Profile.Internal.Features.GetInterests;
using Hpn.Modules.Profile.Internal.Features.GetMyProfile;
using Hpn.Modules.Profile.Internal.Features.GetPublicProfile;
using Hpn.Modules.Profile.Internal.Features.UpdateProfileInterests;
using Hpn.Modules.Profile.Internal.Features.UpdateProfileStatus;
using Hpn.Modules.Profile.Internal.Features.UpsertProfile;
using Hpn.Modules.Profile.Internal.Persistence;
using Hpn.SharedKernel.Modules;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hpn.Modules.Profile;

/// <summary>
/// Composition root for the Profile module. The host calls these two methods and
/// nothing else reaches inside (backbone §5.3, §5.4).
/// </summary>
public static class ProfileModule
{
    public static IServiceCollection AddProfileModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ProfileDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Postgres"))
                   .UseSnakeCaseNamingConvention());

        services.AddScoped<IModuleInitializer, ProfileModuleInitializer>();
        services.AddScoped<IProfileApi, ProfileApi>();
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<UpsertProfileHandler>();
        services.AddScoped<GetMyProfileHandler>();
        services.AddScoped<GetPublicProfileHandler>();
        services.AddScoped<GetInterestsHandler>();
        services.AddScoped<UpdateProfileInterestsHandler>();
        services.AddScoped<UpdateProfileStatusHandler>();

        services.AddValidatorsFromAssemblyContaining<UpsertProfileValidator>(ServiceLifetime.Scoped, includeInternalTypes: true);

        return services;
    }

    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapUpsertProfile();
        endpoints.MapGetMyProfile();
        endpoints.MapGetPublicProfile();
        endpoints.MapGetInterests();
        endpoints.MapUpdateProfileInterests();
        endpoints.MapUpdateProfileStatus();

        return endpoints;
    }
}
