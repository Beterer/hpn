using Hpn.Modules.Admin.Internal.Audit;
using Hpn.Modules.Moderation.Contracts;
using Hpn.Modules.Profile.Contracts;
using Hpn.SharedKernel.Auth;

namespace Hpn.Modules.Admin.Internal.Features.ApplyProfileAction;

internal sealed record ApplyProfileActionResult(
    ApplyProfileActionResponse? Response,
    bool ProfileMissing)
{
    public static ApplyProfileActionResult MissingProfile() => new(null, ProfileMissing: true);

    public static ApplyProfileActionResult Success(ApplyProfileActionResponse response) =>
        new(response, ProfileMissing: false);
}

internal sealed class ApplyProfileActionHandler(
    ICurrentUser currentUser,
    IProfileApi profileApi,
    IModerationApi moderationApi,
    AdminAuditWriter auditWriter)
{
    public async Task<ApplyProfileActionResult> HandleAsync(
        Guid profileId,
        ApplyProfileActionRequest request,
        CancellationToken cancellationToken)
    {
        var targetUserId = await profileApi.GetUserIdForProfileAsync(profileId, cancellationToken);
        if (targetUserId is null)
        {
            return ApplyProfileActionResult.MissingProfile();
        }

        var adminUserId = currentUser.RequireUserId();
        var action = request.Action.Trim().ToLowerInvariant();

        // Audit the decision *before* applying it, so an applied moderation action is
        // never left unaudited (§11) — the two writes span separate module schemas and
        // can't share a transaction. An over-recorded audit on a failed action is the
        // safe direction to fail.
        var audit = await auditWriter.WriteAsync(
            adminUserId,
            $"profile_action:{action}",
            $"profile:{profileId}",
            new
            {
                targetProfileId = profileId,
                targetUserId = targetUserId.Value,
                action,
                request.Reason,
            },
            cancellationToken);

        var decision = await moderationApi.ApplyAdminProfileActionAsync(
            profileId,
            targetUserId.Value,
            request.Action,
            request.Reason,
            adminUserId,
            cancellationToken);

        return ApplyProfileActionResult.Success(new ApplyProfileActionResponse(
            audit.Id,
            profileId,
            targetUserId.Value,
            decision.Action,
            decision.ExpiresAt));
    }
}
