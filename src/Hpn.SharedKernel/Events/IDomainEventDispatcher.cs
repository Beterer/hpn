namespace Hpn.SharedKernel.Events;

/// <summary>
/// Dispatches a domain event to all registered handlers synchronously, in the
/// caller's DI scope. v1 has no async/out-of-process path (backbone §3.3, §12).
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);

    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
