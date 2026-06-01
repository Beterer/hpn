using Hpn.Modules.Photo.Contracts.Dtos;

namespace Hpn.Modules.Photo.Contracts;

/// <summary>
/// Read-only surface other modules may use. Photo writes stay in Photo command
/// handlers; binaries remain in object storage, never caller modules.
/// </summary>
public interface IPhotoApi
{
    Task<IReadOnlyCollection<PhotoDto>> GetProfilePhotosAsync(
        Guid profileId,
        CancellationToken cancellationToken = default);

    Task<PhotoDto?> GetPhotoAsync(Guid photoId, CancellationToken cancellationToken = default);

    Task<PhotoDto?> GetPrimaryPhotoAsync(Guid profileId, CancellationToken cancellationToken = default);
}
