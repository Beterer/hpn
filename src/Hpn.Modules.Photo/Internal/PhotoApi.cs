using Hpn.Modules.Photo.Contracts;
using Hpn.Modules.Photo.Contracts.Dtos;
using Hpn.Modules.Photo.Internal.Domain;
using Hpn.Modules.Photo.Internal.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Photo.Internal;

internal sealed class PhotoApi(PhotoDbContext dbContext) : IPhotoApi
{
    public async Task<IReadOnlyCollection<PhotoDto>> GetProfilePhotosAsync(
        Guid profileId,
        CancellationToken cancellationToken = default) =>
        await dbContext.Photos
            .AsNoTracking()
            .Where(p => p.ProfileId == profileId)
            .OrderBy(p => p.Position)
            .Select(p => ToDto(p))
            .ToArrayAsync(cancellationToken);

    public async Task<PhotoDto?> GetPhotoAsync(Guid photoId, CancellationToken cancellationToken = default) =>
        await dbContext.Photos
            .AsNoTracking()
            .Where(p => p.Id == photoId)
            .Select(p => ToDto(p))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PhotoDto?> GetPrimaryPhotoAsync(Guid profileId, CancellationToken cancellationToken = default) =>
        await dbContext.Photos
            .AsNoTracking()
            .Where(p => p.ProfileId == profileId && p.IsPrimary && p.Status == PhotoStatus.Ready)
            .Select(p => ToDto(p))
            .FirstOrDefaultAsync(cancellationToken);

    private static PhotoDto ToDto(ProfilePhoto photo) => new(
        photo.Id,
        photo.ProfileId,
        photo.Position,
        photo.IsPrimary,
        photo.Status.ToStorageValue(),
        photo.Width,
        photo.Height,
        photo.CreatedAt);
}
