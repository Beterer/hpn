using Hpn.SharedKernel.Events;

namespace Hpn.Modules.Photo.Contracts.Events;

public sealed record PhotoRemoved(
    Guid PhotoId,
    Guid ProfileId,
    DateTimeOffset OccurredAt) : IDomainEvent;
