namespace Hpn.Modules.Appreciation.Internal.Features.SubmitAppreciation;

// The client picks a specific trait from the flattened cloud (ADR-025); the
// server derives the category from the trait, so the request carries only TraitId.
internal sealed record SubmitAppreciationRequest(
    Guid ReceiverProfileId,
    Guid TraitId,
    Guid? PhotoId);
