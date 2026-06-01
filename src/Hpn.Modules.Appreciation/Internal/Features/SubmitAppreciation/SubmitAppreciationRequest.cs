namespace Hpn.Modules.Appreciation.Internal.Features.SubmitAppreciation;

internal sealed record SubmitAppreciationRequest(
    Guid ReceiverProfileId,
    Guid CategoryId,
    Guid? PhotoId);
