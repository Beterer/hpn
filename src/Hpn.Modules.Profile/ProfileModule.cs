using FluentValidation;
using Hpn.Modules.Profile.Contracts;
using Hpn.Modules.Profile.Internal;
using Hpn.Modules.Profile.Internal.AccountData;
using Hpn.Modules.Profile.Internal.Development;
using Hpn.Modules.Profile.Internal.Features.GetInterests;
using Hpn.Modules.Profile.Internal.Features.GetMyProfile;
using Hpn.Modules.Profile.Internal.Features.GetPublicProfile;
using Hpn.Modules.Profile.Internal.Features.ManageBlocks;
using Hpn.Modules.Profile.Internal.Features.UpdateLocation;
using Hpn.Modules.Profile.Internal.Features.UpdateProfileInterests;
using Hpn.Modules.Profile.Internal.Features.UpdateProfileStatus;
using Hpn.Modules.Profile.Internal.Features.UpdateVisibilitySettings;
using Hpn.Modules.Profile.Internal.Features.UpsertProfile;
using Hpn.Modules.Profile.Internal.Moderation;
using Hpn.Modules.Profile.Internal.Persistence;
using Hpn.SharedKernel.Accounts;
using Hpn.SharedKernel.Development;
using Hpn.SharedKernel.Events;
using Hpn.SharedKernel.Moderation;
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
        services.AddScoped<IDevelopmentDataSeeder, ProfileDevelopmentDataSeeder>();
        services.AddScoped<IDevelopmentDataSeeder, ProfileActivationDevelopmentDataSeeder>();
        services.AddScoped<IProfileApi, ProfileApi>();
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<UpsertProfileHandler>();
        services.AddScoped<GetMyProfileHandler>();
        services.AddScoped<GetPublicProfileHandler>();
        services.AddScoped<GetInterestsHandler>();
        services.AddScoped<UpdateProfileInterestsHandler>();
        services.AddScoped<UpdateProfileStatusHandler>();
        services.AddScoped<UpdateVisibilitySettingsHandler>();
        services.AddScoped<UpdateLocationHandler>();
        services.AddScoped<ManageBlocksHandler>();

        // Account export/erasure slice + soft-delete reaction (backbone §10.5).
        services.AddScoped<IAccountDataContributor, ProfileDataContributor>();
        services.AddScoped<IDomainEventHandler<AccountDeletionRequested>, ProfileAccountDeletionHandler>();

        // Moderation decisions reflect into the profile lifecycle so the feed honours
        // them (§6.7, §10.3). One handler services all three shared moderation events.
        services.AddScoped<IDomainEventHandler<UserRestricted>, ProfileModerationStatusHandler>();
        services.AddScoped<IDomainEventHandler<UserBanned>, ProfileModerationStatusHandler>();
        services.AddScoped<IDomainEventHandler<UserCleared>, ProfileModerationStatusHandler>();

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
        endpoints.MapUpdateVisibilitySettings();
        endpoints.MapUpdateLocation();
        endpoints.MapManageBlocks();

        return endpoints;
    }
}
