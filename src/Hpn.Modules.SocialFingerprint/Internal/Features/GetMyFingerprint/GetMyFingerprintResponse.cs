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

// A specific recurring trait (ADR-025): the named trait, its share, and the hue of
// the category it belongs to so the client can colour it like the radar.
internal sealed record FingerprintTraitResponse(
    Guid TraitId,
    string Slug,
    string Label,
    double Share,
    string Phrasing,
    int Hue);

internal sealed record FingerprintTrendPointResponse(
    DateOnly PeriodStart,
    int SampleSize,
    IReadOnlyCollection<FingerprintTraitResponse> TopTraits);
