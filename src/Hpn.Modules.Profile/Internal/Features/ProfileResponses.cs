using Hpn.Modules.Profile.Internal.Domain;
using Hpn.Modules.Profile.Internal.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Profile.Internal.Features;

internal sealed record InterestResponse(Guid Id, string Slug, string Label);

internal sealed record VisibilityPreferencesResponse(
    bool ShowOnlyOutsideCountry,
    bool HideFromCountry,
    int? MinDistanceKm,
    bool WomenForWomen,
    bool VerifiedOnly,
    bool Paused,
    bool HiddenFromGuests);

internal sealed record ProfileResponse(
    Guid Id,
    string DisplayName,
    string Gender,
    string? SelfDescribeText,
    string? CountryCode,
    string? Bio,
    bool Verified,
    string Status,
    IReadOnlyCollection<InterestResponse> Interests,
    VisibilityPreferencesResponse VisibilityPreferences);

internal sealed record PublicProfileResponse(
    Guid Id,
    string DisplayName,
    string Gender,
    string? SelfDescribeText,
    string? CountryCode,
    string? Bio,
    bool Verified,
    IReadOnlyCollection<InterestResponse> Interests);

internal static class ProfileResponses
{
    public static async Task<ProfileResponse?> LoadMineAsync(
        ProfileDbContext dbContext,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.Profiles
            .AsNoTracking()
            .Include(p => p.ProfileInterests)
            .ThenInclude(pi => pi.Interest)
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        return profile is null ? null : ToResponse(profile);
    }

    public static ProfileResponse ToResponse(UserProfile profile) => new(
        profile.Id,
        profile.DisplayName,
        profile.Gender.ToStorageValue(),
        profile.SelfDescribeText,
        profile.CountryCode,
        profile.Bio,
        profile.Verified,
        profile.Status.ToStorageValue(),
        ToInterests(profile),
        new VisibilityPreferencesResponse(
            profile.VisibilityPreferences.ShowOnlyOutsideCountry,
            profile.VisibilityPreferences.HideFromCountry,
            profile.VisibilityPreferences.MinDistanceKm,
            profile.VisibilityPreferences.WomenForWomen,
            profile.VisibilityPreferences.VerifiedOnly,
            profile.VisibilityPreferences.Paused,
            profile.VisibilityPreferences.HiddenFromGuests));

    public static PublicProfileResponse ToPublicResponse(UserProfile profile) => new(
        profile.Id,
        profile.DisplayName,
        profile.Gender.ToStorageValue(),
        profile.SelfDescribeText,
        profile.CountryCode,
        profile.Bio,
        profile.Verified,
        ToInterests(profile));

    private static IReadOnlyCollection<InterestResponse> ToInterests(UserProfile profile) =>
        profile.ProfileInterests
            .Select(pi => pi.Interest)
            .OrderBy(i => i.Label, StringComparer.OrdinalIgnoreCase)
            .Select(i => new InterestResponse(i.Id, i.Slug, i.Label))
            .ToArray();
}
