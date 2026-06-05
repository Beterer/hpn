using FluentAssertions;
using Hpn.Modules.Feed.Internal.Features.GetNext;
using Xunit;

namespace Hpn.Modules.Feed.Tests;

/// <summary>
/// The coarse distance bands shown on a feed card (backbone §10.4): only ever a
/// bucket, never an exact number, and never a coordinate.
/// </summary>
public sealed class DistanceBucketsTests
{
    // Bucharest as the reference point.
    private const double Lat = 44.4;
    private const double Lng = 26.1;

    [Theory]
    [InlineData(44.4, 26.1, DistanceBuckets.Nearby)]        // same coarse cell
    [InlineData(44.7, 26.1, DistanceBuckets.Under50Km)]     // ~33 km north
    [InlineData(45.4, 26.1, DistanceBuckets.Between50And200Km)] // ~111 km north
    [InlineData(46.8, 23.6, DistanceBuckets.Over200Km)]     // ~Cluj, ~250 km
    public void Buckets_by_distance_when_both_points_known(double lat, double lng, string expected)
    {
        DistanceBuckets.For(Lat, Lng, lat, lng).Should().Be(expected);
    }

    [Fact]
    public void Is_null_when_a_point_is_missing()
    {
        DistanceBuckets.For(Lat, Lng, candidateLat: null, candidateLng: null).Should().BeNull();
        DistanceBuckets.For(null, null, null, null).Should().BeNull();
    }
}
