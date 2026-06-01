using Hpn.Modules.Appreciation.Contracts.Dtos;
using Hpn.Modules.Appreciation.Internal.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Appreciation.Internal.Features.GetAppreciationCategories;

internal sealed class GetAppreciationCategoriesHandler(AppreciationDbContext dbContext)
{
    public async Task<IReadOnlyCollection<AppreciationCategoryDto>> HandleAsync(
        CancellationToken cancellationToken) =>
        await dbContext.AppreciationCategories
            .AsNoTracking()
            .Where(c => c.Active)
            .OrderBy(c => c.SortOrder)
            .Select(c => new AppreciationCategoryDto(c.Id, c.Slug, c.Label, c.SortOrder))
            .ToArrayAsync(cancellationToken);
}
