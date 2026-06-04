using Hpn.Modules.Appreciation.Contracts.Dtos;
using Hpn.Modules.Appreciation.Internal.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Appreciation.Internal.Features.GetAppreciationCategories;

// Shared loader for the active category catalog with nested traits (ADR-025).
// Both GET /appreciation-categories and IAppreciationApi.GetCategoriesAsync hand
// the client the same shape — the categories carry the hue, the traits are the
// flattened picker — so the projection lives in one place.
internal static class AppreciationCatalogQuery
{
    public static async Task<IReadOnlyCollection<AppreciationCategoryDto>> LoadAsync(
        AppreciationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var categories = await dbContext.AppreciationCategories
            .AsNoTracking()
            .Where(c => c.Active)
            .OrderBy(c => c.SortOrder)
            .Select(c => new { c.Id, c.Slug, c.Label, c.SortOrder, c.Hue })
            .ToArrayAsync(cancellationToken);

        var traits = await dbContext.AppreciationTraits
            .AsNoTracking()
            .Where(t => t.Active)
            .OrderBy(t => t.SortOrder)
            .Select(t => new AppreciationTraitDto(t.Id, t.CategoryId, t.Slug, t.Label, t.SortOrder))
            .ToArrayAsync(cancellationToken);

        var traitsByCategory = traits
            .GroupBy(t => t.CategoryId)
            .ToDictionary(g => g.Key, g => (IReadOnlyCollection<AppreciationTraitDto>)g.ToArray());

        return categories
            .Select(c => new AppreciationCategoryDto(
                c.Id,
                c.Slug,
                c.Label,
                c.SortOrder,
                c.Hue,
                traitsByCategory.GetValueOrDefault(c.Id, Array.Empty<AppreciationTraitDto>())))
            .ToArray();
    }
}
