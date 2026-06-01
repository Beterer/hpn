namespace Hpn.Modules.Photo.Internal.Nsfw;

internal sealed record NsfwScanResult(bool Passed, string Value)
{
    public static NsfwScanResult Pass { get; } = new(Passed: true, Value: "pass");
}
