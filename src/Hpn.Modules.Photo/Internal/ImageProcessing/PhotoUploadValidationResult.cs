namespace Hpn.Modules.Photo.Internal.ImageProcessing;

internal sealed record PhotoUploadValidationResult(bool IsValid, string? Problem)
{
    public static PhotoUploadValidationResult Valid { get; } = new(true, null);

    public static PhotoUploadValidationResult Invalid(string problem) => new(false, problem);
}
