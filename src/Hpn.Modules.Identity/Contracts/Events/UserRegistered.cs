using Hpn.SharedKernel.Events;

namespace Hpn.Modules.Identity.Contracts.Events;

/// <summary>
/// Raised the first time an account is created for an email (backbone §6.1).
/// Other modules subscribe to bootstrap their own per-user state; dispatched
/// in-process, synchronously, inside the registering transaction (§10.7).
/// </summary>
public sealed record UserRegistered(Guid UserId, string Email) : IDomainEvent;
