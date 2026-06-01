namespace Hpn.SharedKernel.Events;

/// <summary>
/// Handles a domain event synchronously, in-process, within the raising
/// request's transaction. Handlers must be fast (backbone §10.7).
/// </summary>
public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
