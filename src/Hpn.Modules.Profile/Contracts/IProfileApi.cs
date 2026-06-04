using Hpn.Modules.Profile.Contracts.Dtos;

namespace Hpn.Modules.Profile.Contracts;

/// <summary>
/// The only surface other modules may call into Profile through (backbone §6.2,
/// §3.3). Writes stay inside Profile's command handlers.
/// </summary>
public interface IProfileApi
{
    Task<Guid?> GetProfileIdForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Reverse of <see cref="GetProfileIdForUserAsync"/>: the account that owns a
    /// profile. Moderation needs it to turn a report's <c>target_profile_id</c> into the
    /// user id its restrictions and trust score are keyed by (§6.7, §10.3).</summary>
    Task<Guid?> GetUserIdForProfileAsync(Guid profileId, CancellationToken cancellationToken = default);

    /// <summary>Whether a profile carries the admin-set verified flag (§6.3). A trust-score
    /// input (§10.3), read without the visibility gate that <see cref="GetPublicProfileAsync"/>
    /// applies.</summary>
    Task<bool> IsVerifiedAsync(Guid profileId, CancellationToken cancellationToken = default);

    Task<PublicProfileDto?> GetPublicProfileAsync(
        Guid profileId,
        Guid viewerId,
        CancellationToken cancellationToken = default);

    Task<bool> IsVisibleToAsync(Guid profileId, Guid viewerId, CancellationToken cancellationToken = default);

    /// <summary>Visibility check for a viewer who may be a signed-out guest. When
    /// <paramref name="enforceGuestRestrictions"/> is true, a profile that opted out of
    /// guest visibility (<c>hidden_from_guests</c>) is treated as not visible, so the
    /// opt-out holds on direct photo/appreciation access and not only in the feed.</summary>
    Task<bool> IsVisibleToAsync(
        Guid profileId,
        Guid viewerId,
        bool enforceGuestRestrictions,
        CancellationToken cancellationToken = default);

    Task<string?> GetStatusAsync(Guid profileId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Guid>> GetBlockedByAsync(Guid viewerId, CancellationToken cancellationToken = default);
}
