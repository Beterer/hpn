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

    Task<AppreciationStyleDto> GetAppreciationStyleAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AppreciationCategoryDto>> GetCategoriesAsync(
        CancellationToken cancellationToken = default);
}
