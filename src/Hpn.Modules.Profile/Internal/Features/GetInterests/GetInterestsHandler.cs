using Hpn.Modules.Profile.Internal.Features;
using Hpn.Modules.Profile.Internal.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Profile.Internal.Features.GetInterests;

internal sealed class GetInterestsHandler(ProfileDbContext dbContext)
{
    public async Task<IReadOnlyCollection<InterestResponse>> HandleAsync(CancellationToken cancellationToken) =>
        await dbContext.Interests
            .AsNoTracking()
            .OrderBy(i => i.Label)
            .Select(i => new InterestResponse(i.Id, i.Slug, i.Label))
            .ToArrayAsync(cancellationToken);
}
