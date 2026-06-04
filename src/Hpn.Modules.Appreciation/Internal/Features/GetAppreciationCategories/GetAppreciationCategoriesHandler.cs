using Hpn.Modules.Appreciation.Contracts.Dtos;
using Hpn.Modules.Appreciation.Internal.Persistence;

namespace Hpn.Modules.Appreciation.Internal.Features.GetAppreciationCategories;

internal sealed class GetAppreciationCategoriesHandler(AppreciationDbContext dbContext)
{
    public Task<IReadOnlyCollection<AppreciationCategoryDto>> HandleAsync(
        CancellationToken cancellationToken) =>
        AppreciationCatalogQuery.LoadAsync(dbContext, cancellationToken);
}
