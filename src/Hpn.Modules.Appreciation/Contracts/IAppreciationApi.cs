using Hpn.Modules.Appreciation.Contracts.Dtos;

namespace Hpn.Modules.Appreciation.Contracts;

/// <summary>
/// Public read surface for appreciation state. Writes stay inside the
/// Appreciation module's vertical slices.
/// </summary>
public interface IAppreciationApi
{
    Task<bool> HasAppreciatedAsync(
        Guid senderUserId,
        Guid receiverProfileId,
        CancellationToken cancellationToken = default);

    Task<ReceivedAppreciationSummaryDto> GetReceivedSummaryAsync(
        Guid profileId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Trait-level received counts for a profile (ADR-025), aggregated from the
    /// appreciation events. Lets the fingerprint surface specific recurring traits
    /// while the radar stays category-level.
    /// </summary>
    Task<IReadOnlyCollection<AppreciationTraitCountDto>> GetReceivedTraitSummaryAsync(
        Guid profileId,
        CancellationToken cancellationToken = default);

    Task<AppreciationStyleDto> GetAppreciationStyleAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AppreciationCategoryDto>> GetCategoriesAsync(
        CancellationToken cancellationToken = default);
}
