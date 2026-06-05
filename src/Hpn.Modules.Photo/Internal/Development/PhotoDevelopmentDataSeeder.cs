using System.Security.Cryptography;
using System.Text;
using Hpn.Modules.Photo.Internal.Domain;
using Hpn.Modules.Photo.Internal.ImageProcessing;
using Hpn.Modules.Photo.Internal.Persistence;
using Hpn.Modules.Photo.Internal.Storage;
using Hpn.SharedKernel.Development;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Hpn.Modules.Photo.Internal.Development;

internal sealed class PhotoDevelopmentDataSeeder(
    PhotoDbContext dbContext,
    ImageProcessor imageProcessor,
    IObjectStore objectStore,
    IOptions<PhotoUploadOptions> uploadOptions,
    IHostEnvironment hostEnvironment,
    TimeProvider timeProvider) : IDevelopmentDataSeeder
{
    private readonly PhotoUploadOptions _uploadOptions = uploadOptions.Value;

    public int Phase => 30;

    public async Task SeedAsync(DevelopmentSeedContext context, CancellationToken cancellationToken = default)
    {
        var imageFiles = ResolveImageFiles(context.Options.ImageDirectory);
        if (imageFiles.Count == 0)
        {
            throw new InvalidOperationException(
                $"Development seed image directory '{context.Options.ImageDirectory}' contains no .jpg files.");
        }

        var keys = new[] { "test" }
            .Concat(Enumerable.Range(0, Math.Max(0, context.Options.CandidateCount)).Select(context.CandidateKey))
            .ToArray();

        for (var profileIndex = 0; profileIndex < keys.Length; profileIndex++)
        {
            var key = keys[profileIndex];
            var seededProfile = context.GetProfile(key);
            var desiredCount = DesiredPhotoCount(key, profileIndex);
            var photos = new List<DevelopmentSeedPhoto>(desiredCount);

            for (var position = 0; position < desiredCount; position++)
            {
                var imageOffset = StableIndex($"photo-image-offset:{key}", imageFiles.Count);
                var file = imageFiles[(imageOffset + position) % imageFiles.Count];
                var photo = await EnsurePhotoAsync(
                    key,
                    seededProfile.ProfileId,
                    position,
                    file,
                    cancellationToken);
                photos.Add(photo);
            }

            context.SetPhotos(key, photos);
        }
    }

    private async Task<DevelopmentSeedPhoto> EnsurePhotoAsync(
        string profileKey,
        Guid profileId,
        int position,
        string imagePath,
        CancellationToken cancellationToken)
    {
        var photoId = StableGuid($"photo:{profileId:N}:{position}");
        var existing = await dbContext.Photos
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == photoId, cancellationToken);

        if (existing is not null && existing.ProfileId != profileId)
        {
            throw new InvalidOperationException($"Development seed photo id collision for '{photoId}'.");
        }

        var keys = existing is null
            ? PhotoObjectKeys.ForPhoto(profileId, photoId)
            : new PhotoObjectKeys(existing.OriginalKey, existing.DisplayKey, existing.ThumbKey);

        if (existing is not null && await HasAllVariantsAsync(keys, cancellationToken))
        {
            return new DevelopmentSeedPhoto(profileKey, profileId, photoId, position);
        }

        var processed = await ProcessAsync(imagePath, cancellationToken);

        await StoreVariantsAsync(keys, processed, cancellationToken);

        if (existing is null)
        {
            var positionConflict = await dbContext.Photos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProfileId == profileId && p.Position == position, cancellationToken);
            if (positionConflict is not null)
            {
                return new DevelopmentSeedPhoto(profileKey, profileId, positionConflict.Id, positionConflict.Position);
            }

            var photo = ProfilePhoto.CreateReady(
                photoId,
                profileId,
                position,
                keys.OriginalKey,
                keys.DisplayKey,
                keys.ThumbKey,
                processed.Width,
                processed.Height,
                processed.ContentHash,
                scanResult: "development_seed",
                timeProvider.GetUtcNow(),
                isPrimary: position == 0);
            dbContext.Photos.Add(photo);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new DevelopmentSeedPhoto(profileKey, profileId, photoId, position);
    }

    private async Task<ProcessedPhoto> ProcessAsync(string imagePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(imagePath);
        var formFile = new FormFile(stream, 0, stream.Length, "file", Path.GetFileName(imagePath))
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg",
        };

        return await imageProcessor.ProcessAsync(formFile, cancellationToken);
    }

    private async Task StoreVariantsAsync(
        PhotoObjectKeys keys,
        ProcessedPhoto processed,
        CancellationToken cancellationToken)
    {
        var variants = new[]
        {
            new ObjectVariant(keys.OriginalKey, "image/webp", processed.Original),
            new ObjectVariant(keys.DisplayKey, "image/webp", processed.Display),
            new ObjectVariant(keys.ThumbKey, "image/webp", processed.Thumb),
        };

        foreach (var variant in variants)
        {
            await objectStore.PutAsync(variant, cancellationToken);
        }
    }

    private async Task<bool> HasAllVariantsAsync(PhotoObjectKeys keys, CancellationToken cancellationToken)
    {
        foreach (var key in new[] { keys.OriginalKey, keys.DisplayKey, keys.ThumbKey })
        {
            var stored = await objectStore.GetAsync(key, cancellationToken);
            if (stored is null)
            {
                return false;
            }

            await stored.DisposeAsync();
        }

        return true;
    }

    private int DesiredPhotoCount(string profileKey, int profileIndex)
    {
        var max = Math.Clamp(_uploadOptions.MaxPhotosPerProfile, 1, 5);
        return 1 + StableIndex($"photo-count:{profileKey}:{profileIndex}", max);
    }

    private IReadOnlyList<string> ResolveImageFiles(string configuredPath)
    {
        var candidates = Path.IsPathRooted(configuredPath)
            ? [configuredPath]
            : new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), configuredPath),
                Path.Combine(hostEnvironment.ContentRootPath, configuredPath),
                Path.Combine(hostEnvironment.ContentRootPath, "..", "..", configuredPath),
                Path.Combine(AppContext.BaseDirectory, configuredPath),
            };

        var directory = candidates
            .Select(Path.GetFullPath)
            .FirstOrDefault(Directory.Exists);
        if (directory is null)
        {
            return [];
        }

        return Directory.EnumerateFiles(directory, "*.jpg")
            .OrderBy(NumericImageOrder)
            .ThenBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static int NumericImageOrder(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        var marker = name.LastIndexOf('_');
        return marker >= 0 && int.TryParse(name[(marker + 1)..], out var value) ? value : int.MaxValue;
    }

    private static Guid StableGuid(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes("hpn-development-seed:" + value));
        return new Guid(hash[..16]);
    }

    private static int StableIndex(string value, int length)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes("hpn-development-seed-random:" + value));
        var bytes = hash[..8];
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        return (int)Math.Floor(BitConverter.ToUInt64(bytes) / ((double)ulong.MaxValue + 1) * length);
    }
}
