using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace Hpn.Modules.Photo.Internal.Storage;

internal sealed class S3ObjectStore(IAmazonS3 s3, IOptions<PhotoStorageOptions> options) : IObjectStore
{
    private readonly PhotoStorageOptions _options = options.Value;

    public async Task PutAsync(ObjectVariant variant, CancellationToken cancellationToken)
    {
        using var content = new MemoryStream(variant.Bytes, writable: false);
        await s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = variant.Key,
            InputStream = content,
            ContentType = variant.ContentType,
        }, cancellationToken);
    }

    public async Task<StoredObject?> GetAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            var response = await s3.GetObjectAsync(_options.BucketName, key, cancellationToken);
            return new StoredObject(response.ResponseStream, response.Headers.ContentType ?? "application/octet-stream");
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            await s3.DeleteObjectAsync(_options.BucketName, key, cancellationToken);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
        }
    }
}
