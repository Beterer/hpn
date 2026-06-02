using Hpn.Modules.Appreciation.Internal;
using Hpn.Modules.Appreciation.Internal.AccountData;
using Hpn.Modules.Appreciation.Internal.Features.GetAppreciationStyle;
using Hpn.Modules.Appreciation.Internal.Features.GetAppreciationCategories;
using Hpn.Modules.Appreciation.Internal.Features.GetReceivedAppreciation;
using Hpn.Modules.Appreciation.Internal.Features.SubmitAppreciation;
using Hpn.Modules.Appreciation.Internal.Persistence;
using Hpn.Modules.Appreciation.Contracts;
using Hpn.Modules.Appreciation.Contracts.Events;
using Hpn.SharedKernel.Accounts;
using Hpn.SharedKernel.Events;
using Hpn.SharedKernel.Modules;
using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

        // Backs the cached platform-wide totals in the appreciation-style read.
        services.AddMemoryCache();

        services.AddScoped<IModuleInitializer, AppreciationModuleInitializer>();
        services.AddScoped<IAppreciationApi, AppreciationApi>();
        services.AddScoped<IDomainEventHandler<AppreciationCreated>, AppreciationCounterProjectionHandler>();
        services.AddScoped<GetAppreciationCategoriesHandler>();
        services.AddScoped<GetAppreciationStyleHandler>();
        services.AddScoped<GetReceivedAppreciationHandler>();
        services.AddScoped<SubmitAppreciationHandler>();
        services.AddScoped<IAccountDataContributor, AppreciationDataContributor>();
        services.TryAddSingleton(TimeProvider.System);

        services.AddValidatorsFromAssemblyContaining<SubmitAppreciationValidator>(
            ServiceLifetime.Scoped,
            includeInternalTypes: true);

        return services;
    }

    public static IEndpointRouteBuilder MapAppreciationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGetAppreciationCategories();
        endpoints.MapGetAppreciationStyle();
        endpoints.MapGetReceivedAppreciation();
        endpoints.MapSubmitAppreciation();

        return endpoints;
    }
}
