namespace Hpn.Modules.Appreciation.Internal.Features.GetReceivedAppreciation;

internal sealed record GetReceivedAppreciationResponse(
    Guid ProfileId,
    string Headline,
    string Summary,
    int Total,
    IReadOnlyCollection<ReceivedAppreciationCategoryResponse> Categories,
    IReadOnlyCollection<ReceivedAppreciationEventResponse> Events);

internal sealed record ReceivedAppreciationCategoryResponse(
    Guid CategoryId,
    string Slug,
    string Label,
    int Count,
    string Phrasing);

internal sealed record ReceivedAppreciationEventResponse(
    Guid Id,
    Guid CategoryId,
    string CategorySlug,
    string CategoryLabel,
    Guid? PhotoId,
    DateTimeOffset CreatedAt,
    string Phrasing);
