namespace Hpn.Modules.SocialFingerprint.Internal.Features.GetMyFingerprint;

internal sealed record GetMyFingerprintResponse(
    string Status,
    int Needed,
    Guid? ProfileId,
    string Headline,
    string Summary,
    int SampleSize,
    IReadOnlyCollection<FingerprintDistributionItemResponse> Distribution,
    IReadOnlyCollection<FingerprintTraitResponse> TopTraits,
    IReadOnlyCollection<FingerprintTrendPointResponse> Trend);

internal sealed record FingerprintDistributionItemResponse(
    Guid CategoryId,
    string Slug,
    string Label,
    double Share,
    string Phrasing);

internal sealed record FingerprintTraitResponse(
    Guid CategoryId,
    string Slug,
    string Label,
    double Share,
    string Phrasing);

internal sealed record FingerprintTrendPointResponse(
    DateOnly PeriodStart,
    int SampleSize,
    IReadOnlyCollection<FingerprintTraitResponse> TopTraits);
