namespace Hpn.Modules.Photo.Internal.Nsfw;

internal sealed class NoOpNsfwScanner : INsfwScanner
{
    public Task<NsfwScanResult> ScanAsync(NsfwScanInput input, CancellationToken cancellationToken) =>
        Task.FromResult(NsfwScanResult.Pass);
}
