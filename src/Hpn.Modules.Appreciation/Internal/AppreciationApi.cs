using Hpn.Modules.Appreciation.Contracts;
using Hpn.Modules.Appreciation.Contracts.Dtos;
using Hpn.Modules.Appreciation.Internal.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Appreciation.Internal;

internal sealed class AppreciationApi(AppreciationDbContext dbContext) : IAppreciationApi
{
    public Task<bool> HasAppreciatedAsync(
        Guid senderUserId,
        Guid receiverProfileId,
        CancellationToken cancellationToken = default) =>
        dbContext.AppreciationEvents
            .AsNoTracking()
            .AnyAsync(
                e => e.SenderUserId == senderUserId && e.ReceiverProfileId == receiverProfileId,
                cancellationToken);

    public async Task<ReceivedAppreciationSummaryDto> GetReceivedSummaryAsync(
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        var categories = await dbContext.ReceivedAppreciationStats
            .AsNoTracking()
            .Where(s => s.ReceiverProfileId == profileId)
            .Join(
                dbContext.AppreciationCategories.AsNoTracking(),
                stat => stat.CategoryId,
                category => category.Id,
                (stat, category) => new
                {
                    category.Id,
                    category.Slug,
                    category.Label,
                    stat.Count,
                })
            .OrderBy(c => c.Label)
            .Select(c => new AppreciationCategoryCountDto(c.Id, c.Slug, c.Label, c.Count))
            .ToArrayAsync(cancellationToken);

        return new ReceivedAppreciationSummaryDto(profileId, categories.Sum(c => c.Count), categories);
    }

    public async Task<AppreciationStyleDto> GetAppreciationStyleAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var categories = await dbContext.GivenAppreciationStats
            .AsNoTracking()
            .Where(s => s.SenderUserId == userId)
            .Join(
                dbContext.AppreciationCategories.AsNoTracking(),
                stat => stat.CategoryId,
                category => category.Id,
                (stat, category) => new
                {
                    category.Id,
                    category.Slug,
                    category.Label,
                    stat.Count,
                })
            .OrderBy(c => c.Label)
            .Select(c => new AppreciationCategoryCountDto(c.Id, c.Slug, c.Label, c.Count))
            .ToArrayAsync(cancellationToken);

        return new AppreciationStyleDto(userId, categories.Sum(c => c.Count), categories);
    }

    public async Task<IReadOnlyCollection<AppreciationCategoryDto>> GetCategoriesAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.AppreciationCategories
            .AsNoTracking()
            .Where(c => c.Active)
            .OrderBy(c => c.SortOrder)
            .Select(c => new AppreciationCategoryDto(c.Id, c.Slug, c.Label, c.SortOrder))
            .ToArrayAsync(cancellationToken);
}
