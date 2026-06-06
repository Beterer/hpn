using Hpn.SharedKernel.Events;

namespace Hpn.Modules.Appreciation.Contracts.Events;

public sealed record AppreciationCreated(
    Guid AppreciationId,
    Guid SenderUserId,
    Guid ReceiverProfileId,
    Guid CategoryId,
    Guid TraitId,
    Guid? PhotoId,
    string TraitLabel,
    string CategorySlug,
    // Natural, receiver-facing sentence ("Someone felt your good vibe.") — the
    // canonical perception copy, so consumers never re-template the raw label.
    string Phrasing,
    DateTimeOffset OccurredAt) : IDomainEvent;
