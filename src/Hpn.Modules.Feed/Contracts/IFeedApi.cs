using Hpn.Modules.Feed.Contracts.Dtos;

namespace Hpn.Modules.Feed.Contracts;

/// <summary>
/// The only surface other modules may call into Feed through (backbone §6.5).
/// Ranking lives behind an internal strategy; callers see a stable contract that
/// never changes when the algorithm does.
/// </summary>
public interface IFeedApi
{
    /// <summary>
    /// The next batch of eligible profiles for a viewer, already ranked by the
    /// active strategy. <paramref name="viewerId"/> is the viewer's user id.
    /// </summary>
    Task<IReadOnlyList<FeedProfileDto>> GetNextAsync(
        Guid viewerId,
        int batchSize,
        CancellationToken cancellationToken = default);
}
