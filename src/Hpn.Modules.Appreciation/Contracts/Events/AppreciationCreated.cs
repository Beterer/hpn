using Hpn.SharedKernel.Events;

namespace Hpn.Modules.Appreciation.Contracts.Events;

public sealed record AppreciationCreated(
    Guid AppreciationId,
    Guid SenderUserId,
    Guid ReceiverProfileId,
    Guid CategoryId,
    Guid TraitId,
    Guid? PhotoId,
    DateTimeOffset OccurredAt) : IDomainEvent;
