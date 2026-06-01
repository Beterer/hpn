using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace Hpn.SharedKernel.Events;

/// <summary>
/// Resolves and invokes every <see cref="IDomainEventHandler{TEvent}"/> for an
/// event's runtime type, in order, awaiting each. Handlers are resolved from the
/// service provider injected here, so they share the current request scope (and
/// therefore the same DbContext/transaction). Synchronous and in-process by
/// design — see backbone §3.3.
/// </summary>
public sealed class InProcessDomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, IDomainEvent, CancellationToken, Task>> Invokers = new();

    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        var invoke = Invokers.GetOrAdd(domainEvent.GetType(), BuildInvoker);
        await invoke(serviceProvider, domainEvent, cancellationToken);
    }

    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);
        foreach (var domainEvent in domainEvents)
        {
            await DispatchAsync(domainEvent, cancellationToken);
        }
    }

    private static Func<IServiceProvider, IDomainEvent, CancellationToken, Task> BuildInvoker(Type eventType)
    {
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
        var handleMethod = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!;

        return async (provider, domainEvent, cancellationToken) =>
        {
            foreach (var handler in provider.GetServices(handlerType))
            {
                if (handler is null)
                {
                    continue;
                }

                await (Task)handleMethod.Invoke(handler, [domainEvent, cancellationToken])!;
            }
        };
    }
}
