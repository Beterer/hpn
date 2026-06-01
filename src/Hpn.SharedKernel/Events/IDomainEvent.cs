namespace Hpn.SharedKernel.Events;

/// <summary>
/// Marker for an in-process domain event. Events are raised inside a handler's
/// transaction and dispatched synchronously (backbone §3.3, §10.7). There is no
/// background worker in v1.
/// </summary>
public interface IDomainEvent
{
}
