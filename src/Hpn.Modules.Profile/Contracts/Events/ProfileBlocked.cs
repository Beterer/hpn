using Hpn.SharedKernel.Events;

namespace Hpn.Modules.Profile.Contracts.Events;

public sealed record ProfileBlocked(Guid BlockerUserId, Guid BlockedUserId, DateTimeOffset OccurredAt) : IDomainEvent;
