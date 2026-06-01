namespace Hpn.Modules.Appreciation.Contracts.Dtos;

public sealed record AppreciationStyleDto(
    Guid UserId,
    int Total,
    IReadOnlyCollection<AppreciationCategoryCountDto> Categories);
