using Hpn.Modules.Photo.Internal.Domain;
using Hpn.Modules.Photo.Internal.Persistence;
using Hpn.Modules.Profile.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Photo.Internal;

internal sealed class ReadyPhotoActivationRequirement(PhotoDbContext dbContext) : IProfileActivationRequirement
{
    private static readonly ProfileActivationRequirementResult MissingReadyPhoto = new(
        Satisfied: false,
        ProblemType: "https://hpn.dev/problems/profile-photo-required",
        Title: "Photo required",
        Detail: "Add at least one ready photo before activating your profile.");

    public async Task<ProfileActivationRequirementResult> CheckAsync(
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        var hasReadyPhoto = await dbContext.Photos
            .AsNoTracking()
            .AnyAsync(p => p.ProfileId == profileId && p.Status == PhotoStatus.Ready, cancellationToken);

        return hasReadyPhoto ? ProfileActivationRequirementResult.Pass : MissingReadyPhoto;
    }
}
