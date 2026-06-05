using Hpn.Modules.Photo.Internal.Persistence;
using Hpn.Modules.Photo.Internal.Storage;
using Hpn.SharedKernel.Accounts;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Photo.Internal.AccountData;

/// <summary>
/// Photo's slice of account export + erasure (§10.5). Erasure also removes the
/// binaries from object storage — the part the row-only deletes elsewhere can't
/// reach (§10.2).
/// </summary>
internal sealed class PhotoDataContributor(
    PhotoDbContext dbContext,
    IObjectStore objectStore) : IAccountDataContributor
{
    public string Section => "photos";

    public async Task<object?> ExportAsync(AccountScope scope, CancellationToken cancellationToken = default)
    {
        if (scope.ProfileId is not { } profileId)
        {
            return null;
        }

        var photos = await dbContext.Photos
            .AsNoTracking()
            .Where(p => p.ProfileId == profileId)
            .OrderBy(p => p.Position)
            .Select(p => new
            {
                p.Id,
                p.Position,
                p.IsPrimary,
                Status = p.Status.ToString().ToLowerInvariant(),
                p.Width,
                p.Height,
                p.ContentHash,
                p.CreatedAt,
            })
            .ToArrayAsync(cancellationToken);

        return photos.Length == 0 ? null : photos;
    }

    public async Task EraseAsync(AccountScope scope, CancellationToken cancellationToken = default)
    {
        if (scope.ProfileId is not { } profileId)
        {
            return;
        }

        var keys = await dbContext.Photos
            .AsNoTracking()
            .Where(p => p.ProfileId == profileId)
            .Select(p => new { p.OriginalKey, p.DisplayKey, p.ThumbKey })
            .ToArrayAsync(cancellationToken);

        await dbContext.Photos
            .Where(p => p.ProfileId == profileId)
            .ExecuteDeleteAsync(cancellationToken);

        // Rows are gone; drop the blobs best-effort (an orphan blob is the safe
        // failure mode, never a row pointing at missing content — mirrors the
        // single-photo delete path).
        foreach (var key in keys)
        {
            await TryDeleteAsync(key.OriginalKey, cancellationToken);
            await TryDeleteAsync(key.DisplayKey, cancellationToken);
            await TryDeleteAsync(key.ThumbKey, cancellationToken);
        }
    }

    private async Task TryDeleteAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            await objectStore.DeleteAsync(key, cancellationToken);
        }
        catch (Exception)
        {
            // Swallow — the database row is already gone; orphan blobs are recoverable.
        }
    }
}
