namespace Hpn.Modules.Feed.Contracts.Dtos;

/// <summary>
/// One feed card: an eligible profile a viewer may appreciate (backbone §6.5,
/// §8 Feed). Carries only the appreciation-relevant surface — no status,
/// counts, scores, or rankings (§2). Photo URLs point at the visibility-checked
/// public photo-serving endpoint.
/// </summary>
public sealed record FeedProfileDto(
    Guid ProfileId,
    string DisplayName,
    string Gender,
    string? SelfDescribeText,
    bool Verified,
    IReadOnlyList<FeedPhotoDto> Photos,
    // Interest labels (tags) the profile chose. Already public via the public
    // profile; shown as quiet chips on the card.
    IReadOnlyList<string> Interests,
    // Coarse distance band only — never an exact number (§10.4). Null when there is
    // no usable location to measure. One of: nearby, under_50km, 50_200km, 200km_plus.
    string? DistanceBucket);

public sealed record FeedPhotoDto(
    Guid PhotoId,
    int Position,
    int Width,
    int Height,
    string DisplayUrl,
    string ThumbUrl);
