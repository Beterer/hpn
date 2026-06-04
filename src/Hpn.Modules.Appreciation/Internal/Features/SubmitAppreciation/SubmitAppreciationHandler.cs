using Hpn.Modules.Appreciation.Contracts.Events;
using Hpn.Modules.Appreciation.Internal.Domain;
using Hpn.Modules.Appreciation.Internal.Persistence;
using Hpn.Modules.Photo.Contracts;
using Hpn.Modules.Profile.Contracts;
using Hpn.SharedKernel.Auth;
using Hpn.SharedKernel.Events;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Hpn.Modules.Appreciation.Internal.Features.SubmitAppreciation;

internal sealed record SubmitAppreciationResult(
    SubmitAppreciationResponse? Response,
    bool Replayed,
    bool InvalidIdempotencyKey,
    bool ProfileMissing,
    bool SelfAppreciation,
    bool ReceiverNotVisible,
    bool CategoryMissing,
    bool PhotoMismatch,
    bool IdempotencyConflict,
    bool Duplicate)
{
    public static SubmitAppreciationResult FromResponse(SubmitAppreciationResponse response, bool replayed) =>
        new(
            response,
            Replayed: replayed,
            InvalidIdempotencyKey: false,
            ProfileMissing: false,
            SelfAppreciation: false,
            ReceiverNotVisible: false,
            CategoryMissing: false,
            PhotoMismatch: false,
            IdempotencyConflict: false,
            Duplicate: false);

    public static SubmitAppreciationResult Failure(
        bool invalidIdempotencyKey = false,
        bool profileMissing = false,
        bool selfAppreciation = false,
        bool receiverNotVisible = false,
        bool categoryMissing = false,
        bool photoMismatch = false,
        bool idempotencyConflict = false,
        bool duplicate = false) =>
        new(
            null,
            Replayed: false,
            InvalidIdempotencyKey: invalidIdempotencyKey,
            ProfileMissing: profileMissing,
            SelfAppreciation: selfAppreciation,
            ReceiverNotVisible: receiverNotVisible,
            CategoryMissing: categoryMissing,
            PhotoMismatch: photoMismatch,
            IdempotencyConflict: idempotencyConflict,
            Duplicate: duplicate);
}

internal sealed class SubmitAppreciationHandler(
    AppreciationDbContext dbContext,
    ICurrentUser currentUser,
    IProfileApi profileApi,
    IPhotoApi photoApi,
    TimeProvider timeProvider,
    IDomainEventDispatcher eventDispatcher)
{
    public async Task<SubmitAppreciationResult> HandleAsync(
        SubmitAppreciationRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var senderActorId = currentUser.RequireActorId();
        var normalizedKey = NormalizeIdempotencyKey(idempotencyKey);
        if (normalizedKey is null)
        {
            return SubmitAppreciationResult.Failure(invalidIdempotencyKey: true);
        }

        var replay = await TryReplayAsync(senderActorId, normalizedKey, request, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        if (currentUser.UserId is { } senderUserId)
        {
            var senderProfileId = await profileApi.GetProfileIdForUserAsync(senderUserId, cancellationToken);
            if (senderProfileId is null)
            {
                return SubmitAppreciationResult.Failure(profileMissing: true);
            }

            if (senderProfileId == request.ReceiverProfileId)
            {
                return SubmitAppreciationResult.Failure(selfAppreciation: true);
            }
        }

        var category = await dbContext.AppreciationCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId && c.Active, cancellationToken);
        if (category is null)
        {
            return SubmitAppreciationResult.Failure(categoryMissing: true);
        }

        // Visibility (active, not paused, no block in either direction) is the
        // deliberate interaction gate. Audience preferences (women_for_women,
        // verified_only, country/distance) are feed-shaping filters, not hard
        // interaction blocks — consistent with GET /profiles/{id}, which is also
        // visibility-gated only. A profile reachable by id is appreciable.
        var visible = await profileApi.IsVisibleToAsync(
            request.ReceiverProfileId,
            senderActorId,
            enforceGuestRestrictions: currentUser.ActorKind == ActorKind.Guest,
            cancellationToken);
        if (!visible)
        {
            return SubmitAppreciationResult.Failure(receiverNotVisible: true);
        }

        if (request.PhotoId is { } photoId)
        {
            var photo = await photoApi.GetPhotoAsync(photoId, cancellationToken);
            if (photo is null ||
                photo.ProfileId != request.ReceiverProfileId ||
                !string.Equals(photo.Status, "ready", StringComparison.Ordinal))
            {
                return SubmitAppreciationResult.Failure(photoMismatch: true);
            }
        }

        var alreadyAppreciated = await dbContext.AppreciationEvents
            .AsNoTracking()
            .AnyAsync(
                e => e.SenderUserId == senderActorId &&
                     e.ReceiverProfileId == request.ReceiverProfileId &&
                     e.CategoryId == request.CategoryId,
                cancellationToken);
        if (alreadyAppreciated)
        {
            return SubmitAppreciationResult.Failure(duplicate: true);
        }

        var now = timeProvider.GetUtcNow();
        var appreciation = AppreciationEvent.Create(
            senderActorId,
            request.ReceiverProfileId,
            request.CategoryId,
            request.PhotoId,
            normalizedKey,
            now);

        dbContext.AppreciationEvents.Add(appreciation);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await eventDispatcher.DispatchAsync(
                new AppreciationCreated(
                    appreciation.Id,
                    appreciation.SenderUserId,
                    appreciation.ReceiverProfileId,
                    appreciation.CategoryId,
                    appreciation.PhotoId,
                    appreciation.CreatedAt),
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            await transaction.RollbackAsync(cancellationToken);
            dbContext.Entry(appreciation).State = EntityState.Detached;
            return await ResolveUniqueViolationAsync(senderActorId, normalizedKey, request, cancellationToken);
        }

        var response = ToResponse(appreciation, category.Slug, category.Label, replayed: false);
        return SubmitAppreciationResult.FromResponse(response, replayed: false);
    }

    private async Task<SubmitAppreciationResult?> TryReplayAsync(
        Guid senderUserId,
        string idempotencyKey,
        SubmitAppreciationRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.AppreciationEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.SenderUserId == senderUserId && e.IdempotencyKey == idempotencyKey,
                cancellationToken);
        if (existing is null)
        {
            return null;
        }

        if (!existing.MatchesRequest(request.ReceiverProfileId, request.CategoryId, request.PhotoId))
        {
            return SubmitAppreciationResult.Failure(idempotencyConflict: true);
        }

        var category = await dbContext.AppreciationCategories
            .AsNoTracking()
            .FirstAsync(c => c.Id == existing.CategoryId, cancellationToken);

        return SubmitAppreciationResult.FromResponse(
            ToResponse(existing, category.Slug, category.Label, replayed: true),
            replayed: true);
    }

    private async Task<SubmitAppreciationResult> ResolveUniqueViolationAsync(
        Guid senderUserId,
        string idempotencyKey,
        SubmitAppreciationRequest request,
        CancellationToken cancellationToken)
    {
        var replay = await TryReplayAsync(senderUserId, idempotencyKey, request, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        var duplicate = await dbContext.AppreciationEvents
            .AsNoTracking()
            .AnyAsync(
                e => e.SenderUserId == senderUserId &&
                     e.ReceiverProfileId == request.ReceiverProfileId &&
                     e.CategoryId == request.CategoryId,
                cancellationToken);

        return duplicate
            ? SubmitAppreciationResult.Failure(duplicate: true)
            : throw new DbUpdateException("Unexpected unique violation while submitting appreciation.");
    }

    private static SubmitAppreciationResponse ToResponse(
        AppreciationEvent appreciation,
        string categorySlug,
        string categoryLabel,
        bool replayed) =>
        new(
            appreciation.Id,
            appreciation.ReceiverProfileId,
            appreciation.CategoryId,
            categorySlug,
            categoryLabel,
            appreciation.PhotoId,
            appreciation.CreatedAt,
            replayed,
            NextProfileUnlocked: true);

    private static string? NormalizeIdempotencyKey(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) || trimmed.Length > 128 ? null : trimmed;
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
