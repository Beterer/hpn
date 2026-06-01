using Hpn.SharedKernel.Events;

namespace Hpn.Modules.Photo.Contracts.Events;

public sealed record PhotoUploaded(
    Guid PhotoId,
    Guid ProfileId,
    int Position,
    DateTimeOffset OccurredAt) : IDomainEvent;
