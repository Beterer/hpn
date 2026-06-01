namespace Hpn.Modules.Photo.Internal.Domain;

internal static class PhotoFormat
{
    public static string ToStorageValue(this PhotoStatus status) => status switch
    {
        PhotoStatus.Processing => "processing",
        PhotoStatus.Ready => "ready",
        PhotoStatus.Rejected => "rejected",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown photo status."),
    };

    public static PhotoStatus ParseStatus(string value) => value.Trim().ToLowerInvariant() switch
    {
        "processing" => PhotoStatus.Processing,
        "ready" => PhotoStatus.Ready,
        "rejected" => PhotoStatus.Rejected,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown photo status."),
    };
}
