namespace Hpn.Modules.Moderation.Contracts;

/// <summary>
/// The vocabulary of admin moderation decisions (backbone §7.1
/// <c>moderation.action_type</c>). Centralized so the admin-facing validation and the
/// decision dispatch in <see cref="IModerationApi.ApplyAdminProfileActionAsync"/> can
/// never disagree on what is a valid action.
/// </summary>
public static class ModerationActions
{
    public const string Warn = "warn";
    public const string TempRestrict = "temp_restrict";
    public const string Ban = "ban";
    public const string Clear = "clear";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Warn,
        TempRestrict,
        Ban,
        Clear,
    };
}
