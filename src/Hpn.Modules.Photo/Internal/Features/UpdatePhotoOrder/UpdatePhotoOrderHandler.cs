using Hpn.Modules.Photo.Internal.Persistence;
using Hpn.Modules.Profile.Contracts;
using Hpn.SharedKernel.Auth;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Photo.Internal.Features.UpdatePhotoOrder;

internal sealed record UpdatePhotoOrderResult(
    IReadOnlyCollection<PhotoResponse> Photos,
    bool ProfileMissing,
    bool InvalidPhotoSet);

internal sealed class UpdatePhotoOrderHandler(
    PhotoDbContext dbContext,
    ICurrentUser currentUser,
    IProfileApi profileApi)
{
    public async Task<UpdatePhotoOrderResult> HandleAsync(
        UpdatePhotoOrderRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        var profileId = await profileApi.GetProfileIdForUserAsync(userId, cancellationToken);
        if (profileId is null)
        {
            return new UpdatePhotoOrderResult([], ProfileMissing: true, InvalidPhotoSet: false);
        }

        var photos = await dbContext.Photos
            .Where(p => p.ProfileId == profileId.Value)
            .ToArrayAsync(cancellationToken);

        var requested = request.PhotoIds.ToArray();
        if (photos.Length != requested.Length || photos.Select(p => p.Id).Except(requested).Any())
        {
            return new UpdatePhotoOrderResult([], ProfileMissing: false, InvalidPhotoSet: true);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        for (var index = 0; index < photos.Length; index++)
        {
            photos[index].MoveTo(requested.Length + index);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var byId = photos.ToDictionary(p => p.Id);
        for (var index = 0; index < requested.Length; index++)
        {
            byId[requested[index]].MoveTo(index);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var response = photos
            .OrderBy(p => p.Position)
            .Select(PhotoResponses.ToResponse)
            .ToArray();

        return new UpdatePhotoOrderResult(response, ProfileMissing: false, InvalidPhotoSet: false);
    }
}
