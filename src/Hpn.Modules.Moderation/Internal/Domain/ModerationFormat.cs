namespace Hpn.Modules.Moderation.Internal.Domain;

/// <summary>
/// snake_case storage mapping for the moderation enums (backbone §7.1, §5.6). The
/// wire/DB values are the stable contract; the C# names are ours to refactor.
/// </summary>
internal static class ModerationFormat
{
    public static string ToStorageValue(this ReportType type) => type switch
    {
        ReportType.AiGenerated => "ai_generated",
        ReportType.FakeProfile => "fake_profile",
        ReportType.InappropriateContent => "inappropriate_content",
        ReportType.StolenPhotos => "stolen_photos",
        ReportType.Spam => "spam",
        ReportType.Nsfw => "nsfw",
        ReportType.Harassment => "harassment",
        ReportType.Underage => "underage",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown report type."),
    };

    public static ReportType ParseReportType(string value) => value.Trim().ToLowerInvariant() switch
    {
        "ai_generated" => ReportType.AiGenerated,
        "fake_profile" => ReportType.FakeProfile,
        "inappropriate_content" => ReportType.InappropriateContent,
        "stolen_photos" => ReportType.StolenPhotos,
        "spam" => ReportType.Spam,
        "nsfw" => ReportType.Nsfw,
        "harassment" => ReportType.Harassment,
        "underage" => ReportType.Underage,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown report type."),
    };

    public static bool TryParseReportType(string value, out ReportType type)
    {
        try
        {
            type = ParseReportType(value);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            type = default;
            return false;
        }
    }

    public static string ToStorageValue(this ReportStatus status) => status switch
    {
        ReportStatus.Open => "open",
        ReportStatus.Reviewing => "reviewing",
        ReportStatus.Actioned => "actioned",
        ReportStatus.Dismissed => "dismissed",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown report status."),
    };

    public static ReportStatus ParseReportStatus(string value) => value.Trim().ToLowerInvariant() switch
    {
        "open" => ReportStatus.Open,
        "reviewing" => ReportStatus.Reviewing,
        "actioned" => ReportStatus.Actioned,
        "dismissed" => ReportStatus.Dismissed,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown report status."),
    };

    public static string ToStorageValue(this ActionType action) => action switch
    {
        ActionType.Warn => "warn",
        ActionType.TempRestrict => "temp_restrict",
        ActionType.Ban => "ban",
        ActionType.Clear => "clear",
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Unknown action type."),
    };

    public static ActionType ParseActionType(string value) => value.Trim().ToLowerInvariant() switch
    {
        "warn" => ActionType.Warn,
        "temp_restrict" => ActionType.TempRestrict,
        "ban" => ActionType.Ban,
        "clear" => ActionType.Clear,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown action type."),
    };
}
