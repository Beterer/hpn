namespace Hpn.Modules.Photo.Internal.Nsfw;

internal interface INsfwScanner
{
    Task<NsfwScanResult> ScanAsync(NsfwScanInput input, CancellationToken cancellationToken);
}
