using System.Security.Cryptography;
using System.Text;
using Hpn.Modules.Appreciation.Internal.Domain;
using Hpn.Modules.Appreciation.Internal.Persistence;
using Hpn.SharedKernel.Development;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Appreciation.Internal.Development;

internal sealed class AppreciationDevelopmentDataSeeder(
    AppreciationDbContext dbContext,
    TimeProvider timeProvider) : IDevelopmentDataSeeder
{
    public int Phase => 50;

    public async Task SeedAsync(DevelopmentSeedContext context, CancellationToken cancellationToken = default)
    {
        var categories = await dbContext.AppreciationCategories
            .AsNoTracking()
            .Where(c => c.Active)
            .OrderBy(c => c.SortOrder)
            .ToArrayAsync(cancellationToken);
        if (categories.Length == 0)
        {
            throw new InvalidOperationException("Development seed requires appreciation categories.");
        }

        var traits = await dbContext.AppreciationTraits
            .AsNoTracking()
            .Where(t => t.Active)
            .OrderBy(t => t.SortOrder)
            .ToArrayAsync(cancellationToken);
        var traitsByCategory = traits
            .GroupBy(t => t.CategoryId)
            .ToDictionary(g => g.Key, g => g.ToArray());
        if (traitsByCategory.Count == 0)
        {
            throw new InvalidOperationException("Development seed requires appreciation traits.");
        }

        var now = timeProvider.GetUtcNow();
        var affectedReceivers = new HashSet<Guid>();
        var affectedSenders = new HashSet<Guid>();

        var testUser = context.GetUser("test");
        var testProfile = context.GetProfile("test");

        await DeleteExistingSeedEventsAsync(
            testUser.UserId,
            testProfile.ProfileId,
            affectedReceivers,
            affectedSenders,
            cancellationToken);

        var incomingCount = Math.Max(0, context.Options.IncomingAppreciationCount);
        var incomingCategories = BuildWeightedSequence(
            categories,
            incomingCount,
            category => IncomingCategoryWeight(category.Slug),
            "incoming-categories");
        var incomingTraitsByCategory = BuildTraitQueues(
            incomingCategories,
            traitsByCategory,
            trait => IncomingTraitWeight(trait.Slug),
            "incoming-traits");

        for (var i = 0; i < incomingCount; i++)
        {
            var sender = context.GetUser(context.ObserverKey(i));
            var category = incomingCategories[i];
            var trait = incomingTraitsByCategory[category.Id].Dequeue();
            await InsertEventAsync(
                sender.UserId,
                testProfile.ProfileId,
                category.Id,
                trait.Id,
                (Guid?)null,
                $"development-seed-incoming-{i:D2}",
                now.AddMinutes(-incomingCount + i),
                cancellationToken);

            affectedSenders.Add(sender.UserId);
            affectedReceivers.Add(testProfile.ProfileId);
        }

        var outgoingCount = Math.Min(
            Math.Max(0, context.Options.OutgoingAppreciationCount),
            Math.Max(0, context.Options.CandidateCount));
        var outgoingCategories = BuildWeightedSequence(
            categories,
            outgoingCount,
            category => OutgoingCategoryWeight(category.Slug),
            "outgoing-categories");
        var outgoingTraitsByCategory = BuildTraitQueues(
            outgoingCategories,
            traitsByCategory,
            trait => OutgoingTraitWeight(trait.Slug),
            "outgoing-traits");

        for (var i = 0; i < outgoingCount; i++)
        {
            var candidateKey = context.CandidateKey(i);
            var receiver = context.GetProfile(candidateKey);
            var category = outgoingCategories[i];
            var trait = outgoingTraitsByCategory[category.Id].Dequeue();
            await InsertEventAsync(
                testUser.UserId,
                receiver.ProfileId,
                category.Id,
                trait.Id,
                (Guid?)null,
                $"development-seed-outgoing-{i:D2}",
                now.AddMinutes(-outgoingCount + i),
                cancellationToken);

            affectedSenders.Add(testUser.UserId);
            affectedReceivers.Add(receiver.ProfileId);
        }

        foreach (var receiverProfileId in affectedReceivers)
        {
            await RebuildReceivedStatsAsync(receiverProfileId, cancellationToken);
        }

        foreach (var senderUserId in affectedSenders)
        {
            await RebuildGivenStatsAsync(senderUserId, cancellationToken);
        }
    }

    private async Task DeleteExistingSeedEventsAsync(
        Guid testUserId,
        Guid testProfileId,
        HashSet<Guid> affectedReceivers,
        HashSet<Guid> affectedSenders,
        CancellationToken cancellationToken)
    {
        var existingSeedEvents = await dbContext.AppreciationEvents
            .AsNoTracking()
            .Where(e =>
                (e.ReceiverProfileId == testProfileId &&
                 EF.Functions.Like(e.IdempotencyKey, "development-seed-incoming-%")) ||
                (e.SenderUserId == testUserId &&
                 EF.Functions.Like(e.IdempotencyKey, "development-seed-outgoing-%")))
            .Select(e => new { e.SenderUserId, e.ReceiverProfileId })
            .ToArrayAsync(cancellationToken);

        foreach (var seedEvent in existingSeedEvents)
        {
            affectedSenders.Add(seedEvent.SenderUserId);
            affectedReceivers.Add(seedEvent.ReceiverProfileId);
        }

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             DELETE FROM appreciation.appreciation_events
             WHERE
                 (receiver_profile_id = {testProfileId} AND idempotency_key LIKE 'development-seed-incoming-%')
                 OR
                 (sender_user_id = {testUserId} AND idempotency_key LIKE 'development-seed-outgoing-%')
             """,
            cancellationToken);
    }

    private async Task InsertEventAsync(
        Guid senderUserId,
        Guid receiverProfileId,
        Guid categoryId,
        Guid traitId,
        Guid? photoId,
        string idempotencyKey,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO appreciation.appreciation_events
                 (id, sender_user_id, receiver_profile_id, category_id, trait_id, photo_id, idempotency_key, created_at)
             VALUES
                 ({StableGuid($"appreciation:{idempotencyKey}")}, {senderUserId}, {receiverProfileId},
                  {categoryId}, {traitId}, {photoId}, {idempotencyKey}, {createdAt})
             ON CONFLICT (sender_user_id, idempotency_key) DO NOTHING
             """,
            cancellationToken);
    }

    private async Task RebuildReceivedStatsAsync(Guid receiverProfileId, CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM appreciation.received_appreciation_stats WHERE receiver_profile_id = {receiverProfileId}",
            cancellationToken);

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO appreciation.received_appreciation_stats
                 (receiver_profile_id, category_id, count, last_at)
             SELECT receiver_profile_id, category_id, COUNT(*)::int, MAX(created_at)
             FROM appreciation.appreciation_events
             WHERE receiver_profile_id = {receiverProfileId}
             GROUP BY receiver_profile_id, category_id
             """,
            cancellationToken);
    }

    private async Task RebuildGivenStatsAsync(Guid senderUserId, CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM appreciation.given_appreciation_stats WHERE sender_user_id = {senderUserId}",
            cancellationToken);

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO appreciation.given_appreciation_stats
                 (sender_user_id, category_id, count)
             SELECT sender_user_id, category_id, COUNT(*)::int
             FROM appreciation.appreciation_events
             WHERE sender_user_id = {senderUserId}
             GROUP BY sender_user_id, category_id
             """,
            cancellationToken);
    }

    private static Dictionary<Guid, Queue<AppreciationTrait>> BuildTraitQueues(
        IReadOnlyList<AppreciationCategory> categories,
        IReadOnlyDictionary<Guid, AppreciationTrait[]> traitsByCategory,
        Func<AppreciationTrait, int> weightSelector,
        string salt)
    {
        var queues = new Dictionary<Guid, Queue<AppreciationTrait>>();

        foreach (var categoryId in categories.Select(c => c.Id).Distinct())
        {
            if (!traitsByCategory.TryGetValue(categoryId, out var traits) || traits.Length == 0)
            {
                throw new InvalidOperationException("Development seed requires each seeded category to have traits.");
            }

            var count = categories.Count(category => category.Id == categoryId);
            queues[categoryId] = new Queue<AppreciationTrait>(BuildWeightedSequence(
                traits,
                count,
                weightSelector,
                $"{salt}:{categoryId}"));
        }

        return queues;
    }

    private static IReadOnlyList<T> BuildWeightedSequence<T>(
        IReadOnlyList<T> items,
        int count,
        Func<T, int> weightSelector,
        string salt)
    {
        if (count <= 0)
        {
            return [];
        }

        if (items.Count == 0)
        {
            throw new InvalidOperationException("Development seed cannot build a weighted sequence with no items.");
        }

        var weighted = items
            .Select((item, index) => new
            {
                Item = item,
                Index = index,
                Weight = Math.Max(1, weightSelector(item)),
            })
            .ToArray();
        var totalWeight = weighted.Sum(item => item.Weight);
        var allocations = weighted
            .Select(item =>
            {
                var exact = count * item.Weight / (double)totalWeight;
                var baseCount = (int)Math.Floor(exact);
                return new
                {
                    item.Item,
                    item.Index,
                    BaseCount = baseCount,
                    Fraction = exact - baseCount,
                    TieBreaker = StableUnitInterval($"{salt}:quota:{item.Index}"),
                };
            })
            .ToArray();

        var remaining = count - allocations.Sum(item => item.BaseCount);
        var extraIndexes = allocations
            .OrderByDescending(item => item.Fraction)
            .ThenBy(item => item.TieBreaker)
            .Take(remaining)
            .Select(item => item.Index)
            .ToHashSet();

        var expanded = new List<(T Item, double SortKey)>(count);
        foreach (var allocation in allocations)
        {
            var itemCount = allocation.BaseCount + (extraIndexes.Contains(allocation.Index) ? 1 : 0);
            for (var copy = 0; copy < itemCount; copy++)
            {
                expanded.Add((allocation.Item, StableUnitInterval($"{salt}:shuffle:{allocation.Index}:{copy}")));
            }
        }

        return expanded
            .OrderBy(item => item.SortKey)
            .Select(item => item.Item)
            .ToArray();
    }

    private static int IncomingCategoryWeight(string slug) => slug switch
    {
        "physical" => 6,
        "energy" => 5,
        "style" => 4,
        "humor" => 4,
        "mind" => 3,
        "authentic" => 2,
        _ => 1,
    };

    private static int OutgoingCategoryWeight(string slug) => slug switch
    {
        "mind" => 5,
        "authentic" => 4,
        "humor" => 3,
        "energy" => 3,
        "style" => 2,
        "physical" => 1,
        _ => 1,
    };

    private static int IncomingTraitWeight(string slug) => slug switch
    {
        "warm_smile" => 5,
        "good_vibe" => 4,
        "signature_look" => 4,
        "made_me_grin" => 4,
        "thoughtful" => 4,
        "genuine" => 3,
        "calm_presence" => 3,
        "kind_eyes" => 2,
        "natural_glow" => 2,
        "confident" => 2,
        "effortless" => 2,
        "quick_wit" => 2,
        "curious" => 2,
        "grounded" => 2,
        _ => 1,
    };

    private static int OutgoingTraitWeight(string slug) => slug switch
    {
        "thoughtful" => 5,
        "curious" => 4,
        "genuine" => 4,
        "grounded" => 3,
        "calm_presence" => 3,
        "quick_wit" => 3,
        "effortless" => 2,
        "kind_eyes" => 2,
        _ => 1,
    };

    private static double StableUnitInterval(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes("hpn-development-seed-random:" + value));
        var bytes = hash[..8];
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        return BitConverter.ToUInt64(bytes) / ((double)ulong.MaxValue + 1);
    }

    private static Guid StableGuid(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes("hpn-development-seed:" + value));
        return new Guid(hash[..16]);
    }
}
