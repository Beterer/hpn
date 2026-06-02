using FluentValidation;
using Hpn.Modules.Moderation.Contracts;
using Hpn.Modules.Moderation.Internal;
using Hpn.Modules.Moderation.Internal.Actions;
using Hpn.Modules.Moderation.Internal.Features.SubmitReport;
using Hpn.Modules.Moderation.Internal.Persistence;
using Hpn.Modules.Moderation.Internal.Trust;
using Hpn.SharedKernel.Modules;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        services.AddScoped<IModerationApi, ModerationApi>();
        services.TryAddSingleton(TimeProvider.System);

        // Trust + auto-restriction (§10.3) and the report intake slice.
        services.AddScoped<TrustScoreService>();
        services.AddScoped<ReportPressureEvaluator>();
        services.AddScoped<ModerationActionService>();
        services.AddScoped<RestrictionExpiryService>();
        services.AddScoped<SubmitReportHandler>();

        services.AddValidatorsFromAssemblyContaining<SubmitReportValidator>(
            ServiceLifetime.Scoped,
            includeInternalTypes: true);

        return services;
    }

    public static IEndpointRouteBuilder MapModerationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapSubmitReport();

        return endpoints;
    }
}
