using Hpn.Modules.Profile.Contracts.Dtos;

namespace Hpn.Modules.Profile.Contracts;

/// <summary>
/// The only surface other modules may call into Profile through (backbone §6.2,
/// §3.3). Writes stay inside Profile's command handlers.
/// </summary>
public interface IProfileApi
{
    Task<Guid?> GetProfileIdForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<PublicProfileDto?> GetPublicProfileAsync(
        Guid profileId,
        Guid viewerId,
        CancellationToken cancellationToken = default);

    Task<bool> IsVisibleToAsync(Guid profileId, Guid viewerId, CancellationToken cancellationToken = default);

    Task<string?> GetStatusAsync(Guid profileId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Guid>> GetBlockedByAsync(Guid viewerId, CancellationToken cancellationToken = default);
}
