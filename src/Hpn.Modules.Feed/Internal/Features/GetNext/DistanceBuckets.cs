namespace Hpn.Modules.Feed.Internal.Features.GetNext;

/// <summary>
/// Turns two coarse points into a broad distance band for display (backbone §10.4).
/// The UI must never show an exact distance, so the feed only ever carries one of
/// these buckets. Thresholds are launch values and tunable. When either side has no
/// usable coordinate, there is nothing to measure and the band is null.
/// </summary>
internal static class DistanceBuckets
{
    public const string Nearby = "nearby";
    public const string Under50Km = "under_50km";
    public const string Between50And200Km = "50_200km";
    public const string Over200Km = "200km_plus";

    public static string? For(
        double? viewerLat,
        double? viewerLng,
        double? candidateLat,
        double? candidateLng)
    {
        if (viewerLat is double vLat && viewerLng is double vLng &&
            candidateLat is double cLat && candidateLng is double cLng)
        {
            var km = ApproxKm(vLat, vLng, cLat, cLng);
            return km switch
            {
                < 15 => Nearby,
                < 50 => Under50Km,
                < 200 => Between50And200Km,
                _ => Over200Km,
            };
        }

        // No coordinates to measure with — no distance band.
        return null;
    }

    // Equirectangular approximation over the 0.1°-rounded points — matches the
    // eligibility filter's distance math and is accurate enough for coarse bands.
    private static double ApproxKm(double lat1, double lng1, double lat2, double lng2)
    {
        const double earthRadiusKm = 6371.0;
        const double degToRad = System.Math.PI / 180.0;
        var dx = (lng2 - lng1) * degToRad * System.Math.Cos((lat1 + lat2) / 2.0 * degToRad);
        var dy = (lat2 - lat1) * degToRad;
        return earthRadiusKm * System.Math.Sqrt((dx * dx) + (dy * dy));
    }
}
