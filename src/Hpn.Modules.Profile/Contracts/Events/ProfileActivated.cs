using Hpn.SharedKernel.Events;

namespace Hpn.Modules.Profile.Contracts.Events;

public sealed record ProfileActivated(Guid ProfileId, Guid UserId, DateTimeOffset OccurredAt) : IDomainEvent;
