namespace Hpn.Modules.Feed.Internal.Ranking;

/// <summary>
/// v1 ranking (backbone §6.5): uniform random selection within the eligible set.
/// No boosting, no ordering signals — deliberately the simplest thing that proves
/// the seam. Replacing it with priority/fairness/freshness ranking later is a new
/// class registered in DI; nothing else changes.
/// </summary>
internal sealed class RandomWithinEligibleStrategy : IFeedRankingStrategy
{
    public IReadOnlyList<Guid> Select(
        IReadOnlyList<Guid> eligibleProfileIds,
        FeedViewerContext viewer,
        int batchSize)
    {
        if (batchSize <= 0 || eligibleProfileIds.Count == 0)
        {
            return [];
        }

        // Partial Fisher–Yates: shuffle only the prefix we hand back, so picking a
        // small batch from a large eligible pool stays O(batchSize).
        var pool = eligibleProfileIds.ToArray();
        var take = Math.Min(batchSize, pool.Length);
        for (var i = 0; i < take; i++)
        {
            var j = Random.Shared.Next(i, pool.Length);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        return pool[..take];
    }
}
