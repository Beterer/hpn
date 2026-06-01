using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hpn.SharedKernel.Events;

public static class DomainEventServiceCollectionExtensions
{
    /// <summary>
    /// Registers the synchronous in-process domain event dispatcher. Scoped so it
    /// resolves handlers from the active request scope. Modules register their own
    /// <see cref="IDomainEventHandler{TEvent}"/> implementations.
    /// </summary>
    public static IServiceCollection AddDomainEventDispatcher(this IServiceCollection services)
    {
        services.TryAddScoped<IDomainEventDispatcher, InProcessDomainEventDispatcher>();
        return services;
    }
}
