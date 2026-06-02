using Hpn.Modules.Profile.Contracts.Events;
using Hpn.Modules.Profile.Internal.Domain;
using Hpn.Modules.Profile.Internal.Persistence;
using Hpn.SharedKernel.Auth;
using Hpn.SharedKernel.Events;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Profile.Internal.Features.ManageBlocks;

internal enum BlockOutcome
{
    Done,
    TargetMissing,
    CannotBlockSelf,
}

internal sealed class ManageBlocksHandler(
    ProfileDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    IDomainEventDispatcher eventDispatcher)
{
    public async Task<BlockOutcome> BlockAsync(Guid targetProfileId, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();

        var targetUserId = await ResolveUserIdAsync(targetProfileId, cancellationToken);
        if (targetUserId is null)
        {
            return BlockOutcome.TargetMissing;
        }

        if (targetUserId.Value == userId)
        {
            return BlockOutcome.CannotBlockSelf;
        }

        var already = await dbContext.UserBlocks.AnyAsync(
            b => b.BlockerUserId == userId && b.BlockedUserId == targetUserId.Value,
            cancellationToken);
        if (already)
        {
            return BlockOutcome.Done; // idempotent — blocking twice is a no-op
        }

        var now = timeProvider.GetUtcNow();
        dbContext.UserBlocks.Add(UserBlock.Create(userId, targetUserId.Value, now));
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // A concurrent request inserted the same (blocker, blocked) pair first.
            // The block exists either way, so this stays idempotent — no-op, no 500.
            return BlockOutcome.Done;
        }

        await eventDispatcher.DispatchAsync(new ProfileBlocked(userId, targetUserId.Value, now), cancellationToken);
        return BlockOutcome.Done;
    }

    public async Task<BlockOutcome> UnblockAsync(Guid targetProfileId, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();

        var targetUserId = await ResolveUserIdAsync(targetProfileId, cancellationToken);
        if (targetUserId is null)
        {
            return BlockOutcome.TargetMissing;
        }

        await dbContext.UserBlocks
            .Where(b => b.BlockerUserId == userId && b.BlockedUserId == targetUserId.Value)
            .ExecuteDeleteAsync(cancellationToken);

        return BlockOutcome.Done; // idempotent — removing an absent block is a no-op
    }

    public async Task<IReadOnlyCollection<BlockedProfileResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();

        // Read model join inside the owning module: blocked user ids → their profiles.
        return await (
            from block in dbContext.UserBlocks.AsNoTracking()
            where block.BlockerUserId == userId
            join profile in dbContext.Profiles.AsNoTracking()
                on block.BlockedUserId equals profile.UserId
            orderby profile.DisplayName
            select new BlockedProfileResponse(profile.Id, profile.DisplayName))
            .ToArrayAsync(cancellationToken);
    }

    private Task<Guid?> ResolveUserIdAsync(Guid profileId, CancellationToken cancellationToken) =>
        dbContext.Profiles
            .AsNoTracking()
            .Where(p => p.Id == profileId)
            .Select(p => (Guid?)p.UserId)
            .FirstOrDefaultAsync(cancellationToken);
}
