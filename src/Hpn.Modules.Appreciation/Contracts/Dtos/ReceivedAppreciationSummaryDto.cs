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
