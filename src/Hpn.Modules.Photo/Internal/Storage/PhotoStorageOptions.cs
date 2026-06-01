namespace Hpn.Modules.Photo.Internal.Storage;

internal sealed class PhotoStorageOptions
{
    public string BucketName { get; init; } = "hpn-photos";
    public string ServiceUrl { get; init; } = string.Empty;
    public string AccessKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public string Region { get; init; } = "us-east-1";
    public bool ForcePathStyle { get; init; } = true;
}
