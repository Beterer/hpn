namespace Hpn.Modules.Photo.Internal.Storage;

internal interface IObjectStore
{
    Task PutAsync(ObjectVariant variant, CancellationToken cancellationToken);

    Task<StoredObject?> GetAsync(string key, CancellationToken cancellationToken);

    Task DeleteAsync(string key, CancellationToken cancellationToken);
}

internal sealed record StoredObject(Stream Content, string ContentType) : IAsyncDisposable
{
    public ValueTask DisposeAsync()
    {
        return Content.DisposeAsync();
    }
}
