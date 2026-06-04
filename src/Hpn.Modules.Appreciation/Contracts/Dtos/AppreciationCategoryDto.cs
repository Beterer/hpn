namespace Hpn.Modules.Appreciation.Contracts.Dtos;

public sealed record AppreciationCategoryDto(
    Guid Id,
    string Slug,
    string Label,
    int SortOrder,
    int Hue,
    IReadOnlyCollection<AppreciationTraitDto> Traits);

public sealed record AppreciationTraitDto(
    Guid Id,
    Guid CategoryId,
    string Slug,
    string Label,
    int SortOrder);
