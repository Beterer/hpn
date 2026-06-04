namespace Hpn.Modules.Appreciation.Internal.Features.GetReceivedAppreciation;

internal sealed record GetReceivedAppreciationResponse(
    Guid ProfileId,
    string Headline,
    string Summary,
    int Total,
    IReadOnlyCollection<ReceivedAppreciationTraitResponse> Traits,
    IReadOnlyCollection<ReceivedAppreciationEventResponse> Events);

// "Ways people describe you" — one card per specific trait (ADR-025), carrying its
// category's hue so the client colours it without a second lookup.
internal sealed record ReceivedAppreciationTraitResponse(
    Guid TraitId,
    string Slug,
    string Label,
    string CategorySlug,
    string CategoryLabel,
    int Hue,
    int Count,
    string Phrasing);

internal sealed record ReceivedAppreciationEventResponse(
    Guid Id,
    Guid TraitId,
    string TraitSlug,
    string TraitLabel,
    string CategorySlug,
    int Hue,
    Guid? PhotoId,
    DateTimeOffset CreatedAt,
    string Phrasing);
