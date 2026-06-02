using Hpn.Modules.Admin.Internal;
using Hpn.Modules.Admin.Internal.Audit;
using Hpn.Modules.Admin.Internal.Auth;
using Hpn.Modules.Admin.Internal.Features.ApplyProfileAction;
using Hpn.Modules.Admin.Internal.Features.GetAdminQueue;
using Hpn.Modules.Admin.Internal.Features.GetAdminReports;
using Hpn.Modules.Admin.Internal.Features.GetAdminStats;
using Hpn.Modules.Admin.Internal.Features.ResolveAppeal;
using Hpn.Modules.Admin.Internal.Persistence;
using Hpn.SharedKernel.Modules;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hpn.Modules.Admin;

/// <summary>
/// Composition root for the Admin module. The host calls these two methods and
/// nothing else reaches inside (backbone §5.3, §5.4).
/// </summary>
public static class AdminModule
{
    public static IServiceCollection AddAdminModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AdminDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Postgres"))
                   .UseSnakeCaseNamingConvention());

        services.AddScoped<IModuleInitializer, AdminModuleInitializer>();
        services.AddScoped<AdminAuditWriter>();
        services.AddScoped<GetAdminQueueHandler>();
        services.AddScoped<GetAdminReportsHandler>();
        services.AddScoped<GetAdminStatsHandler>();
        services.AddScoped<ApplyProfileActionHandler>();
        services.AddScoped<ResolveAppealHandler>();
        services.TryAddSingleton(TimeProvider.System);

        services.AddValidatorsFromAssemblyContaining<ApplyProfileActionValidator>(
            ServiceLifetime.Scoped,
            includeInternalTypes: true);

        return services;
    }

    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints.MapGroup("/admin")
            .RequireAuthorization()
            .WithTags("Admin")
            .AddEndpointFilter<AdminOnlyEndpointFilter>();

        admin.MapGetAdminQueue();
        admin.MapGetAdminReports();
        admin.MapGetAdminStats();
        admin.MapApplyProfileAction();
        admin.MapResolveAppeal();

        return endpoints;
    }
}
