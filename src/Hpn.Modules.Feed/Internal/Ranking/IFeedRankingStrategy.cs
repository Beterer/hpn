namespace Hpn.Modules.Feed.Internal.Ranking;

/// <summary>
/// The volatile half of the feed (backbone §6.5). Eligibility decides which
/// profiles <em>may</em> be shown; the strategy decides which of those eligible
/// candidates are actually shown, and in what order. New ranking behaviour
/// (priority/boosts, freshness, fairness, perception-based ordering, A/B
/// variants) is a <em>new implementation of this interface registered in DI</em> —
/// it must never require touching the eligibility query, the
/// <c>IFeedApi</c> contract, or any caller. Anything a future strategy needs is
/// passed in through <see cref="FeedViewerContext"/> or the candidate inputs.
/// </summary>
internal interface IFeedRankingStrategy
{
    /// <summary>
    /// Choose up to <paramref name="batchSize"/> profiles from the eligible set
    /// and return them in display order.
    /// </summary>
    IReadOnlyList<Guid> Select(
        IReadOnlyList<Guid> eligibleProfileIds,
        FeedViewerContext viewer,
        int batchSize);
}

/// <summary>
/// The signals a ranking strategy is allowed to use about the viewer. Kept small
/// and stable on purpose: extending the strategy surface means adding fields
/// here, not reshaping eligibility or the contract (backbone §6.5).
/// </summary>
internal sealed record FeedViewerContext(
    Guid ProfileId,
    Guid UserId,
    string Gender,
    string? CountryCode,
    bool Verified);
