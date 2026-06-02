namespace Hpn.Modules.Appreciation.Internal.Features.GetAppreciationStyle;

internal sealed record GetAppreciationStyleResponse(
    Guid UserId,
    string Status,
    string Headline,
    string Summary,
    int Total,
    IReadOnlyCollection<AppreciationStyleCategoryResponse> Categories);

internal sealed record AppreciationStyleCategoryResponse(
    Guid CategoryId,
    string Slug,
    string Label,
    int Count,
    double Share,
    double PlatformShare,
    double Difference,
    string Insight);
