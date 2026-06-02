using System.Security.Cryptography;
using System.Text;
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

        var now = timeProvider.GetUtcNow();
        var affectedReceivers = new HashSet<Guid>();
        var affectedSenders = new HashSet<Guid>();

        var testUser = context.GetUser("test");
        var testProfile = context.GetProfile("test");
        var testPhotos = context.GetPhotos("test");

        var incomingCount = Math.Max(0, context.Options.IncomingAppreciationCount);
        for (var i = 0; i < incomingCount; i++)
        {
            var sender = context.GetUser(context.ObserverKey(i));
            var category = categories[i % categories.Length];
            var photoId = testPhotos.Count == 0 ? (Guid?)null : testPhotos[i % testPhotos.Count].PhotoId;
            await InsertEventAsync(
                sender.UserId,
                testProfile.ProfileId,
                category.Id,
                photoId,
                $"development-seed-incoming-{i:D2}",
                now.AddMinutes(-incomingCount + i),
                cancellationToken);

            affectedSenders.Add(sender.UserId);
            affectedReceivers.Add(testProfile.ProfileId);
        }

        var outgoingCount = Math.Min(
            Math.Max(0, context.Options.OutgoingAppreciationCount),
            Math.Max(0, context.Options.CandidateCount));
        for (var i = 0; i < outgoingCount; i++)
        {
            var candidateKey = context.CandidateKey(i);
            var receiver = context.GetProfile(candidateKey);
            var receiverPhotos = context.GetPhotos(candidateKey);
            var category = categories[(i + 3) % categories.Length];
            var photoId = receiverPhotos.Count == 0 ? (Guid?)null : receiverPhotos[0].PhotoId;
            await InsertEventAsync(
                testUser.UserId,
                receiver.ProfileId,
                category.Id,
                photoId,
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

    private async Task InsertEventAsync(
        Guid senderUserId,
        Guid receiverProfileId,
        Guid categoryId,
        Guid? photoId,
        string idempotencyKey,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO appreciation.appreciation_events
                 (id, sender_user_id, receiver_profile_id, category_id, photo_id, idempotency_key, created_at)
             VALUES
                 ({StableGuid($"appreciation:{idempotencyKey}")}, {senderUserId}, {receiverProfileId},
                  {categoryId}, {photoId}, {idempotencyKey}, {createdAt})
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

    private static Guid StableGuid(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes("hpn-development-seed:" + value));
        return new Guid(hash[..16]);
    }
}
