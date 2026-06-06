using Hpn.Modules.Appreciation.Contracts.Events;
using Hpn.Modules.Appreciation.Internal.Domain;
using Hpn.Modules.Appreciation.Internal.Features.GetReceivedAppreciation;
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
    bool TraitMissing,
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
            TraitMissing: false,
            PhotoMismatch: false,
            IdempotencyConflict: false,
            Duplicate: false);

    public static SubmitAppreciationResult Failure(
        bool invalidIdempotencyKey = false,
        bool profileMissing = false,
        bool selfAppreciation = false,
        bool receiverNotVisible = false,
        bool traitMissing = false,
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
            TraitMissing: traitMissing,
            PhotoMismatch: photoMismatch,
            IdempotencyConflict: idempotencyConflict,
            Duplicate: duplicate);
}

// A trait joined with the category it belongs to — the labels the response and
// the AppreciationCreated event carry, plus the denormalized category id.
internal sealed record ResolvedTrait(
    Guid TraitId,
    string TraitSlug,
    string TraitLabel,
    Guid CategoryId,
    string CategorySlug,
    string CategoryLabel);

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

        // The client picks a trait; the category is derived from it (ADR-025) and
        // denormalized onto the event so the category-level projections and the
        // duplicate guard keep working.
        var trait = await ResolveTraitAsync(request.TraitId, activeOnly: true, cancellationToken);
        if (trait is null)
        {
            return SubmitAppreciationResult.Failure(traitMissing: true);
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
                     e.CategoryId == trait.CategoryId,
                cancellationToken);
        if (alreadyAppreciated)
        {
            return SubmitAppreciationResult.Failure(duplicate: true);
        }

        var now = timeProvider.GetUtcNow();
        var appreciation = AppreciationEvent.Create(
            senderActorId,
            request.ReceiverProfileId,
            trait.CategoryId,
            trait.TraitId,
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
                    appreciation.TraitId,
                    appreciation.PhotoId,
                    trait.TraitLabel,
                    trait.CategorySlug,
                    ReceivedAppreciationPhrasing.ForEvent(trait.TraitSlug, trait.TraitLabel),
                    appreciation.CreatedAt),
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            await transaction.RollbackAsync(cancellationToken);
            dbContext.Entry(appreciation).State = EntityState.Detached;
            return await ResolveUniqueViolationAsync(senderActorId, normalizedKey, request, trait, cancellationToken);
        }

        var response = ToResponse(appreciation, trait, replayed: false);
        return SubmitAppreciationResult.FromResponse(response, replayed: false);
    }

    // activeOnly gates the write path (you cannot react with a retired trait);
    // the replay/read path looks one up regardless of active state so an old
    // appreciation still renders if a trait is ever deactivated.
    private async Task<ResolvedTrait?> ResolveTraitAsync(
        Guid traitId,
        bool activeOnly,
        CancellationToken cancellationToken) =>
        await dbContext.AppreciationTraits
            .AsNoTracking()
            .Where(t => t.Id == traitId && (!activeOnly || t.Active))
            .Join(
                dbContext.AppreciationCategories.AsNoTracking(),
                t => t.CategoryId,
                c => c.Id,
                (t, c) => new ResolvedTrait(t.Id, t.Slug, t.Label, c.Id, c.Slug, c.Label))
            .FirstOrDefaultAsync(cancellationToken);

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

        if (!existing.MatchesRequest(request.ReceiverProfileId, request.TraitId, request.PhotoId))
        {
            return SubmitAppreciationResult.Failure(idempotencyConflict: true);
        }

        var trait = await ResolveTraitAsync(existing.TraitId, activeOnly: false, cancellationToken);

        return SubmitAppreciationResult.FromResponse(
            ToResponse(existing, trait!, replayed: true),
            replayed: true);
    }

    private async Task<SubmitAppreciationResult> ResolveUniqueViolationAsync(
        Guid senderUserId,
        string idempotencyKey,
        SubmitAppreciationRequest request,
        ResolvedTrait trait,
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
                     e.CategoryId == trait.CategoryId,
                cancellationToken);

        return duplicate
            ? SubmitAppreciationResult.Failure(duplicate: true)
            : throw new DbUpdateException("Unexpected unique violation while submitting appreciation.");
    }

    private static SubmitAppreciationResponse ToResponse(
        AppreciationEvent appreciation,
        ResolvedTrait trait,
        bool replayed) =>
        new(
            appreciation.Id,
            appreciation.ReceiverProfileId,
            trait.CategoryId,
            trait.CategorySlug,
            trait.CategoryLabel,
            trait.TraitId,
            trait.TraitSlug,
            trait.TraitLabel,
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
