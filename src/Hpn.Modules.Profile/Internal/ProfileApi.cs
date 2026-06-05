using Hpn.Modules.Profile.Contracts;
using Hpn.Modules.Profile.Contracts.Dtos;
using Hpn.Modules.Profile.Internal.Domain;
using Hpn.Modules.Profile.Internal.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Profile.Internal;

/// <summary>
/// Read-only implementation of the cross-module Profile contract. Commands stay
/// in vertical slices; this only projects sanctioned data from the profile schema.
/// </summary>
internal sealed class ProfileApi(ProfileDbContext dbContext) : IProfileApi
{
    public async Task<Guid?> GetProfileIdForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await dbContext.Profiles
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<Guid?> GetUserIdForProfileAsync(Guid profileId, CancellationToken cancellationToken = default) =>
        await dbContext.Profiles
            .AsNoTracking()
            .Where(p => p.Id == profileId)
            .Select(p => (Guid?)p.UserId)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> IsVerifiedAsync(Guid profileId, CancellationToken cancellationToken = default) =>
        dbContext.Profiles
            .AsNoTracking()
            .AnyAsync(p => p.Id == profileId && p.Verified, cancellationToken);

    public async Task<PublicProfileDto?> GetPublicProfileAsync(
        Guid profileId,
        Guid viewerId,
        CancellationToken cancellationToken = default)
    {
        var visible = await IsVisibleToAsync(profileId, viewerId, cancellationToken);
        if (!visible)
        {
            return null;
        }

        return await dbContext.Profiles
            .AsNoTracking()
            .Where(p => p.Id == profileId)
            .Select(p => new PublicProfileDto(
                p.Id,
                p.DisplayName,
                p.Gender.ToStorageValue(),
                p.SelfDescribeText,
                p.Verified,
                p.ProfileInterests
                    .OrderBy(pi => pi.Interest.Label)
                    .Select(pi => new PublicInterestDto(pi.Interest.Id, pi.Interest.Slug, pi.Interest.Label))
                    .ToArray()))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> IsVisibleToAsync(Guid profileId, Guid viewerId, CancellationToken cancellationToken = default) =>
        IsVisibleToAsync(profileId, viewerId, enforceGuestRestrictions: false, cancellationToken);

    public async Task<bool> IsVisibleToAsync(
        Guid profileId,
        Guid viewerId,
        bool enforceGuestRestrictions,
        CancellationToken cancellationToken = default)
    {
        var profile = await dbContext.Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == profileId, cancellationToken);
        if (profile is null)
        {
            return false;
        }

        // A profile that opted out of guest visibility is invisible to signed-out
        // browsers everywhere, not just in the feed (VisibilityPreferences is AutoInclude).
        if (enforceGuestRestrictions && profile.VisibilityPreferences.HiddenFromGuests)
        {
            return false;
        }

        var hasBlock = await HasBlockBetweenUsersAsync(profile.UserId, viewerId, cancellationToken);
        return profile.IsVisibleTo(viewerId, hasBlock);
    }

    public async Task<string?> GetStatusAsync(Guid profileId, CancellationToken cancellationToken = default) =>
        await dbContext.Profiles
            .AsNoTracking()
            .Where(p => p.Id == profileId)
            .Select(p => p.Status.ToStorageValue())
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Guid>> GetBlockedByAsync(
        Guid viewerId,
        CancellationToken cancellationToken = default) =>
        await dbContext.UserBlocks
            .AsNoTracking()
            .Where(b => b.BlockerUserId == viewerId)
            .Select(b => b.BlockedUserId)
            .ToArrayAsync(cancellationToken);

    private Task<bool> HasBlockBetweenUsersAsync(Guid ownerUserId, Guid viewerUserId, CancellationToken cancellationToken) =>
        dbContext.UserBlocks.AnyAsync(
            b => (b.BlockerUserId == ownerUserId && b.BlockedUserId == viewerUserId) ||
                 (b.BlockerUserId == viewerUserId && b.BlockedUserId == ownerUserId),
            cancellationToken);
}
