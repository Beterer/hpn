using Hpn.Modules.Profile.Internal.Domain;
using Hpn.Modules.Profile.Internal.Persistence;
using Hpn.SharedKernel.Accounts;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Profile.Internal.AccountData;

/// <summary>
/// Profile's slice of account export + erasure (backbone §10.5). Touches only the
/// profile schema. Interests and visibility cascade with the profile row at the
/// database level; blocks are keyed on user ids, so they are removed explicitly in
/// both directions.
/// </summary>
internal sealed class ProfileDataContributor(ProfileDbContext dbContext) : IAccountDataContributor
{
    public string Section => "profile";

    public async Task<object?> ExportAsync(AccountScope scope, CancellationToken cancellationToken = default)
    {
        var profile = await dbContext.Profiles
            .AsNoTracking()
            .Include(p => p.ProfileInterests)
            .ThenInclude(pi => pi.Interest)
            .FirstOrDefaultAsync(p => p.UserId == scope.UserId, cancellationToken);

        // Represent blocks by the blocked person's profile id + name (what the owner
        // already sees in their blocked list), never other users' internal user ids.
        var blockedProfiles = await (
            from block in dbContext.UserBlocks.AsNoTracking()
            where block.BlockerUserId == scope.UserId
            join blocked in dbContext.Profiles.AsNoTracking()
                on block.BlockedUserId equals blocked.UserId
            orderby blocked.DisplayName
            select new { blocked.Id, blocked.DisplayName })
            .ToArrayAsync(cancellationToken);

        if (profile is null)
        {
            return blockedProfiles.Length == 0 ? null : new { BlockedProfiles = blockedProfiles };
        }

        var prefs = profile.VisibilityPreferences;
        return new
        {
            profile.Id,
            profile.DisplayName,
            Gender = profile.Gender.ToStorageValue(),
            profile.SelfDescribeText,
            // Internal-only field, but it is the subject's own stored personal data, so
            // it belongs in their export (right of access) even though it never shows in the UI.
            profile.CountryCode,
            profile.Verified,
            Status = profile.Status.ToStorageValue(),
            profile.LocationConsent,
            profile.GeoLat,
            profile.GeoLng,
            profile.CreatedAt,
            Interests = profile.ProfileInterests.Select(pi => pi.Interest.Slug).OrderBy(s => s).ToArray(),
            Visibility = new
            {
                prefs.HideFromCountry,
                prefs.MinDistanceKm,
                prefs.WomenForWomen,
                prefs.VerifiedOnly,
                prefs.Paused,
            },
            BlockedProfiles = blockedProfiles,
        };
    }

    public async Task EraseAsync(AccountScope scope, CancellationToken cancellationToken = default)
    {
        // Both directions: blocks I created and blocks others placed on me.
        await dbContext.UserBlocks
            .Where(b => b.BlockerUserId == scope.UserId || b.BlockedUserId == scope.UserId)
            .ExecuteDeleteAsync(cancellationToken);

        // Deleting the profile cascades interests + visibility_preferences (FK ON DELETE CASCADE).
        await dbContext.Profiles
            .Where(p => p.UserId == scope.UserId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
