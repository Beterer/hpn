namespace Hpn.Modules.Profile.Internal.Domain;

internal static class ProfileFormat
{
    public static string ToStorageValue(this Gender gender) => gender switch
    {
        Gender.Woman => "woman",
        Gender.Man => "man",
        Gender.NonBinary => "non_binary",
        Gender.SelfDescribe => "self_describe",
        _ => throw new ArgumentOutOfRangeException(nameof(gender), gender, "Unknown gender."),
    };

    public static Gender ParseGender(string value) => value.Trim().ToLowerInvariant() switch
    {
        "woman" => Gender.Woman,
        "man" => Gender.Man,
        "non_binary" => Gender.NonBinary,
        "self_describe" => Gender.SelfDescribe,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown gender."),
    };

    public static bool TryParseGender(string value, out Gender gender)
    {
        try
        {
            gender = ParseGender(value);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            gender = default;
            return false;
        }
    }

    public static string ToStorageValue(this ProfileStatus status) => status switch
    {
        ProfileStatus.Draft => "draft",
        ProfileStatus.Active => "active",
        ProfileStatus.Paused => "paused",
        ProfileStatus.UnderReview => "under_review",
        ProfileStatus.Banned => "banned",
        ProfileStatus.Deleted => "deleted",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown profile status."),
    };

    public static ProfileStatus ParseStatus(string value) => value.Trim().ToLowerInvariant() switch
    {
        "draft" => ProfileStatus.Draft,
        "active" => ProfileStatus.Active,
        "paused" => ProfileStatus.Paused,
        "under_review" => ProfileStatus.UnderReview,
        "banned" => ProfileStatus.Banned,
        "deleted" => ProfileStatus.Deleted,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown profile status."),
    };
}
