namespace Hpn.Modules.Profile.Internal.Domain;

internal sealed class UserProfile
{
    private readonly List<ProfileInterest> _profileInterests = [];

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string DisplayName { get; private set; } = null!;
    public Gender Gender { get; private set; }
    public string? SelfDescribeText { get; private set; }
    public string? CountryCode { get; private set; }
    public string? Bio { get; private set; }
    public bool Verified { get; private set; }
    public ProfileStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public IReadOnlyCollection<ProfileInterest> ProfileInterests => _profileInterests;
    public VisibilityPreferences VisibilityPreferences { get; private set; } = null!;

    private UserProfile()
    {
    }

    public static UserProfile Create(
        Guid userId,
        string displayName,
        Gender gender,
        string? selfDescribeText,
        string? countryCode,
        string? bio,
        DateTimeOffset now)
    {
        var profile = new UserProfile
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Status = ProfileStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now,
        };

        profile.UpdateDetails(displayName, gender, selfDescribeText, countryCode, bio, now);
        profile.VisibilityPreferences = VisibilityPreferences.Create(profile.Id);
        return profile;
    }

    public void SetVerified(bool verified, DateTimeOffset now)
    {
        Verified = verified;
        UpdatedAt = now;
    }

    public void UpdateDetails(
        string displayName,
        Gender gender,
        string? selfDescribeText,
        string? countryCode,
        string? bio,
        DateTimeOffset now)
    {
        DisplayName = displayName.Trim();
        Gender = gender;
        SelfDescribeText = gender == Gender.SelfDescribe ? NormalizeOptional(selfDescribeText) : null;
        CountryCode = NormalizeCountryCode(countryCode);
        Bio = NormalizeOptional(bio);
        UpdatedAt = now;
    }

    public bool Activate(DateTimeOffset now)
    {
        if (Status is ProfileStatus.UnderReview or ProfileStatus.Banned or ProfileStatus.Deleted)
        {
            return false;
        }

        Status = ProfileStatus.Active;
        VisibilityPreferences.Resume();
        UpdatedAt = now;
        return true;
    }

    public bool Pause(DateTimeOffset now)
    {
        if (Status != ProfileStatus.Active && Status != ProfileStatus.Paused)
        {
            return false;
        }

        Status = ProfileStatus.Paused;
        VisibilityPreferences.Pause();
        UpdatedAt = now;
        return true;
    }

    public void ReplaceInterests(IReadOnlyCollection<Interest> interests, DateTimeOffset now)
    {
        _profileInterests.Clear();
        foreach (var interest in interests.OrderBy(i => i.Label, StringComparer.OrdinalIgnoreCase))
        {
            _profileInterests.Add(ProfileInterest.Create(Id, interest.Id));
        }

        UpdatedAt = now;
    }

    public bool IsVisibleTo(Guid viewerUserId, bool hasBlockBetweenUsers) =>
        UserId == viewerUserId ||
        (Status == ProfileStatus.Active && !VisibilityPreferences.Paused && !hasBlockBetweenUsers);

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static string? NormalizeCountryCode(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed.ToUpperInvariant();
    }
}
