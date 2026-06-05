using Hpn.Modules.Appreciation.Contracts.Events;
using Hpn.Modules.Notification.Internal;
using Hpn.Modules.Notification.Internal.AccountData;
using Hpn.Modules.Notification.Internal.Features.GetNotificationSummary;
using Hpn.Modules.Notification.Internal.Features.MarkNotificationsSeen;
using Hpn.Modules.Notification.Internal.Persistence;
using Hpn.SharedKernel.Accounts;
using Hpn.SharedKernel.Events;
using Hpn.SharedKernel.Modules;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hpn.Modules.Notification;

public static class NotificationModule
{
    public static IServiceCollection AddNotificationModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<NotificationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Postgres"))
                   .UseSnakeCaseNamingConvention());

        services.AddScoped<IModuleInitializer, NotificationModuleInitializer>();
        services.AddScoped<NotificationWriter>();
        services.AddScoped<IDomainEventHandler<AppreciationCreated>, AppreciationReceivedNotificationHandler>();
        services.AddScoped<GetNotificationSummaryHandler>();
        services.AddScoped<MarkNotificationsSeenHandler>();
        services.AddScoped<IAccountDataContributor, NotificationDataContributor>();
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }

    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGetNotificationSummary();
        endpoints.MapMarkNotificationsSeen();

        return endpoints;
    }
}
