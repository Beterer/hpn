namespace Hpn.Modules.Appreciation.Contracts.Dtos;

public sealed record ReceivedAppreciationSummaryDto(
    Guid ProfileId,
    int Total,
    IReadOnlyCollection<AppreciationCategoryCountDto> Categories);

public sealed record AppreciationCategoryCountDto(
    Guid CategoryId,
    string Slug,
    string Label,
    int Count);

// Trait-level received counts (ADR-025): the specific traits under each category,
// carrying the category slug + hue so a consumer (e.g. the fingerprint's recurring
// traits) can show and colour them without another lookup. Computed from events.
public sealed record AppreciationTraitCountDto(
    Guid TraitId,
    string Slug,
    string Label,
    string CategorySlug,
    int Hue,
    int Count);
