namespace Hpn.Modules.Appreciation.Internal.Features.SubmitAppreciation;

internal sealed record SubmitAppreciationResponse(
    Guid Id,
    Guid ReceiverProfileId,
    Guid CategoryId,
    string CategorySlug,
    string CategoryLabel,
    Guid? PhotoId,
    DateTimeOffset CreatedAt,
    bool Replayed,
    bool NextProfileUnlocked);
