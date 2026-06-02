using System.Text.Json;
using Hpn.Modules.Appreciation.Contracts;
using Hpn.Modules.Profile.Contracts;
using Hpn.Modules.SocialFingerprint.Internal.Domain;
using Hpn.Modules.SocialFingerprint.Internal.Persistence;
using Hpn.SharedKernel.Auth;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.SocialFingerprint.Internal.Features.GetMyFingerprint;

internal sealed record GetMyFingerprintResult(
    GetMyFingerprintResponse? Response,
    bool ProfileMissing)
{
    public static GetMyFingerprintResult Success(GetMyFingerprintResponse response) =>
        new(response, ProfileMissing: false);

    public static GetMyFingerprintResult MissingProfile() =>
        new(null, ProfileMissing: true);
}

internal sealed class GetMyFingerprintHandler(
    ICurrentUser currentUser,
    IProfileApi profileApi,
    IAppreciationApi appreciationApi,
    SocialFingerprintDbContext dbContext,
    TimeProvider timeProvider)
{
    private const string WeeklyPeriod = "weekly";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<GetMyFingerprintResult> HandleAsync(CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        var profileId = await profileApi.GetProfileIdForUserAsync(userId, cancellationToken);
        if (profileId is null)
        {
            return GetMyFingerprintResult.MissingProfile();
        }

        var summary = await appreciationApi.GetReceivedSummaryAsync(profileId.Value, cancellationToken);
        if (summary.Total < FingerprintDistribution.MinimumSampleSize)
        {
            return GetMyFingerprintResult.Success(new GetMyFingerprintResponse(
                "insufficient_data",
                FingerprintDistribution.MinimumSampleSize - summary.Total,
                profileId.Value,
                FingerprintPhrasing.InsufficientHeadline,
                FingerprintPhrasing.InsufficientSummary,
                summary.Total,
                Array.Empty<FingerprintDistributionItemResponse>(),
                Array.Empty<FingerprintTraitResponse>(),
                Array.Empty<FingerprintTrendPointResponse>()));
        }

        var categories = await appreciationApi.GetCategoriesAsync(cancellationToken);
        var distribution = FingerprintDistribution.Build(summary, categories);
        var topTraits = FingerprintDistribution.TopTraits(summary, categories);
        var now = timeProvider.GetUtcNow();
        var periodStart = GetWeekStart(now);

        // Pull recent weekly snapshots once. We reuse them both to decide whether
        // this week still needs saving and to draw the past-weeks part of the trend.
        var snapshots = await dbContext.SocialFingerprintSnapshots
            .AsNoTracking()
            .Where(s => s.ProfileId == profileId.Value && s.Period == WeeklyPeriod)
            .OrderByDescending(s => s.PeriodStart)
            .Take(8)
            .ToArrayAsync(cancellationToken);

        // Opportunistic save: only write this week's snapshot when it isn't there
        // yet, so an ordinary read doesn't serialize + INSERT on every call.
        if (snapshots.All(s => s.PeriodStart != periodStart))
        {
            await WriteWeeklySnapshotAsync(
                profileId.Value,
                periodStart,
                summary.Total,
                distribution,
                topTraits,
                now,
                cancellationToken);
        }

        var trend = BuildTrend(snapshots, periodStart, summary.Total, topTraits);

        return GetMyFingerprintResult.Success(new GetMyFingerprintResponse(
            "ready",
            0,
            profileId.Value,
            FingerprintPhrasing.ReadyHeadline,
            FingerprintPhrasing.ReadySummary,
            summary.Total,
            distribution,
            topTraits,
            trend));
    }

    private async Task WriteWeeklySnapshotAsync(
        Guid profileId,
        DateOnly periodStart,
        int sampleSize,
        IReadOnlyCollection<FingerprintDistributionItemResponse> distribution,
        IReadOnlyCollection<FingerprintTraitResponse> topTraits,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var distributionJson = JsonSerializer.Serialize(distribution, JsonOptions);
        var topTraitsJson = JsonSerializer.Serialize(topTraits, JsonOptions);
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO social_fingerprint.social_fingerprint_snapshots
                 (id, profile_id, period, period_start, sample_size, distribution, top_traits, created_at)
             VALUES
                 ({Guid.CreateVersion7()}, {profileId}, {WeeklyPeriod}, {periodStart}, {sampleSize},
                  CAST({distributionJson} AS jsonb), CAST({topTraitsJson} AS jsonb), {now})
             ON CONFLICT (profile_id, period, period_start) DO NOTHING
             """,
            cancellationToken);
    }

    private static IReadOnlyCollection<FingerprintTrendPointResponse> BuildTrend(
        IReadOnlyCollection<SocialFingerprintSnapshot> snapshots,
        DateOnly currentPeriodStart,
        int liveSampleSize,
        IReadOnlyCollection<FingerprintTraitResponse> liveTopTraits)
    {
        // Past, completed weeks come from saved snapshots. The current
        // (in-progress) week is shown from live data, so the newest point always
        // matches the reading above it instead of a value frozen at the week's
        // first view.
        var pastWeeks = snapshots
            .Where(s => s.PeriodStart < currentPeriodStart)
            .OrderBy(s => s.PeriodStart)
            .Select(s => new FingerprintTrendPointResponse(
                s.PeriodStart,
                s.SampleSize,
                JsonSerializer.Deserialize<FingerprintTraitResponse[]>(s.TopTraits, JsonOptions)
                    ?? Array.Empty<FingerprintTraitResponse>()))
            .TakeLast(7);

        return pastWeeks
            .Append(new FingerprintTrendPointResponse(currentPeriodStart, liveSampleSize, liveTopTraits))
            .ToArray();
    }

    internal static DateOnly GetWeekStart(DateTimeOffset value)
    {
        var date = DateOnly.FromDateTime(value.UtcDateTime.Date);
        var daysSinceMonday = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-daysSinceMonday);
    }
}
