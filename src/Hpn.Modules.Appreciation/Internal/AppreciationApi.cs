using Hpn.Modules.Appreciation.Contracts;
using Hpn.Modules.Appreciation.Contracts.Dtos;
using Hpn.Modules.Appreciation.Internal.Features.GetAppreciationCategories;
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

    public async Task<IReadOnlyCollection<AppreciationTraitCountDto>> GetReceivedTraitSummaryAsync(
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        // Trait counts are read on demand from the events (ADR-025) — there is no
        // trait-level projection table.
        var rows = await dbContext.AppreciationEvents
            .AsNoTracking()
            .Where(e => e.ReceiverProfileId == profileId)
            .Join(
                dbContext.AppreciationTraits.AsNoTracking(),
                e => e.TraitId,
                trait => trait.Id,
                (e, trait) => new { trait.Id, trait.Slug, trait.Label, trait.CategoryId })
            .Join(
                dbContext.AppreciationCategories.AsNoTracking(),
                x => x.CategoryId,
                category => category.Id,
                (x, category) => new { x.Id, x.Slug, x.Label, CategorySlug = category.Slug, category.Hue })
            .GroupBy(x => new { x.Id, x.Slug, x.Label, x.CategorySlug, x.Hue })
            .Select(g => new AppreciationTraitCountDto(g.Key.Id, g.Key.Slug, g.Key.Label, g.Key.CategorySlug, g.Key.Hue, g.Count()))
            .ToArrayAsync(cancellationToken);

        return rows;
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

    public Task<IReadOnlyCollection<AppreciationCategoryDto>> GetCategoriesAsync(
        CancellationToken cancellationToken = default) =>
        AppreciationCatalogQuery.LoadAsync(dbContext, cancellationToken);
}
