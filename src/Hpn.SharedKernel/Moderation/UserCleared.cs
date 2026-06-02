using Hpn.SharedKernel.Events;

namespace Hpn.SharedKernel.Moderation;

/// <summary>
/// Raised when a restriction or ban is lifted (backbone §6.7) — by an admin
/// decision, or automatically when a temporary restriction's window elapses.
/// Profile returns the account to <c>active</c> so it is eligible for the feed
/// again.
/// </summary>
public sealed record UserCleared(Guid UserId, DateTimeOffset OccurredAt) : IDomainEvent;
