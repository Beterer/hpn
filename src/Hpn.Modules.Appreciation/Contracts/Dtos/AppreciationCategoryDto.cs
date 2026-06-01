namespace Hpn.Modules.Appreciation.Contracts.Dtos;

public sealed record AppreciationCategoryDto(
    Guid Id,
    string Slug,
    string Label,
    int SortOrder);
