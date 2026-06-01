using Hpn.Modules.Photo.Contracts.Events;
using Hpn.Modules.Photo.Internal.Domain;
using Hpn.Modules.Photo.Internal.ImageProcessing;
using Hpn.Modules.Photo.Internal.Nsfw;
using Hpn.Modules.Photo.Internal.Persistence;
using Hpn.Modules.Photo.Internal.Storage;
using Hpn.Modules.Profile.Contracts;
using Hpn.SharedKernel.Auth;
using Hpn.SharedKernel.Events;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using SixLabors.ImageSharp;

namespace Hpn.Modules.Photo.Internal.Features.UploadProfilePhoto;

internal sealed record UploadProfilePhotoResult(
    PhotoResponse? Photo,
    bool ProfileMissing,
    bool LimitReached,
    string? ValidationProblem);

internal sealed class UploadProfilePhotoHandler(
    PhotoDbContext dbContext,
    ICurrentUser currentUser,
    IProfileApi profileApi,
    PhotoUploadValidator uploadValidator,
    ImageProcessor imageProcessor,
    INsfwScanner nsfwScanner,
    IObjectStore objectStore,
    IOptions<PhotoUploadOptions> options,
    TimeProvider timeProvider,
    IDomainEventDispatcher eventDispatcher)
{
    private readonly PhotoUploadOptions _options = options.Value;

    public async Task<UploadProfilePhotoResult> HandleAsync(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        var profileId = await profileApi.GetProfileIdForUserAsync(userId, cancellationToken);
        if (profileId is null)
        {
            return new UploadProfilePhotoResult(null, ProfileMissing: true, LimitReached: false, ValidationProblem: null);
        }

        var validation = await uploadValidator.ValidateAsync(file, cancellationToken);
        if (!validation.IsValid)
        {
            return new UploadProfilePhotoResult(
                null,
                ProfileMissing: false,
                LimitReached: false,
                ValidationProblem: validation.Problem);
        }

        var currentCount = await dbContext.Photos
            .CountAsync(p => p.ProfileId == profileId.Value, cancellationToken);
        if (currentCount >= _options.MaxPhotosPerProfile)
        {
            return new UploadProfilePhotoResult(null, ProfileMissing: false, LimitReached: true, ValidationProblem: null);
        }

        ProcessedPhoto processed;
        try
        {
            processed = await imageProcessor.ProcessAsync(file!, cancellationToken);
        }
        catch (Exception ex) when (ex is UnknownImageFormatException or InvalidImageContentException)
        {
            return new UploadProfilePhotoResult(
                null,
                ProfileMissing: false,
                LimitReached: false,
                ValidationProblem: "The uploaded image could not be decoded safely.");
        }

        var scan = await nsfwScanner.ScanAsync(
            new NsfwScanInput(profileId.Value, processed.ContentHash, processed.Display),
            cancellationToken);
        if (!scan.Passed)
        {
            return new UploadProfilePhotoResult(
                null,
                ProfileMissing: false,
                LimitReached: false,
                ValidationProblem: "The uploaded photo could not be accepted.");
        }

        var photoId = Guid.CreateVersion7();
        var keys = PhotoObjectKeys.ForPhoto(profileId.Value, photoId);
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

        var now = timeProvider.GetUtcNow();
        ProfilePhoto photo;
        try
        {
            photo = await PersistWithStablePositionAsync(profileId.Value, photoId, keys, processed, scan.Value, now, cancellationToken);
        }
        catch
        {
            // The blobs are already in object storage; if the row never landed,
            // don't leave them orphaned. Best-effort — the original failure wins.
            await TryDeleteObjectsAsync(keys, cancellationToken);
            throw;
        }

        await eventDispatcher.DispatchAsync(
            new PhotoUploaded(photo.Id, photo.ProfileId, photo.Position, now),
            cancellationToken);

        return new UploadProfilePhotoResult(
            PhotoResponses.ToResponse(photo),
            ProfileMissing: false,
            LimitReached: false,
            ValidationProblem: null);
    }

    /// <summary>
    /// Appends the photo at the next free position, retrying if a concurrent upload
    /// claims the same <c>(profile_id, position)</c> slot. The blob keys are stable
    /// across attempts, so only the row is re-tried.
    /// </summary>
    private async Task<ProfilePhoto> PersistWithStablePositionAsync(
        Guid profileId,
        Guid photoId,
        PhotoObjectKeys keys,
        ProcessedPhoto processed,
        string? scanResult,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        const int maxAttempts = 4;
        for (var attempt = 1; ; attempt++)
        {
            var nextPosition = await dbContext.Photos
                .CountAsync(p => p.ProfileId == profileId, cancellationToken);
            var photo = ProfilePhoto.CreateReady(
                photoId,
                profileId,
                nextPosition,
                keys.OriginalKey,
                keys.DisplayKey,
                keys.ThumbKey,
                processed.Width,
                processed.Height,
                processed.ContentHash,
                scanResult,
                now);

            dbContext.Photos.Add(photo);
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                return photo;
            }
            catch (DbUpdateException ex) when (attempt < maxAttempts && IsPositionConflict(ex))
            {
                dbContext.Entry(photo).State = EntityState.Detached;
            }
        }
    }

    private static bool IsPositionConflict(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    private async Task TryDeleteObjectsAsync(PhotoObjectKeys keys, CancellationToken cancellationToken)
    {
        foreach (var key in new[] { keys.OriginalKey, keys.DisplayKey, keys.ThumbKey })
        {
            try
            {
                await objectStore.DeleteAsync(key, cancellationToken);
            }
            catch (Exception)
            {
                // Swallow — orphan-blob cleanup must never mask the upload failure.
            }
        }
    }
}
