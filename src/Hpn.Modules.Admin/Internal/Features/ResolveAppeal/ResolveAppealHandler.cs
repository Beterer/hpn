using Hpn.Modules.Admin.Internal.Audit;
using Hpn.Modules.Moderation.Contracts;
using Hpn.Modules.Profile.Contracts;
using Hpn.SharedKernel.Auth;

namespace Hpn.Modules.Admin.Internal.Features.ResolveAppeal;

internal sealed record ResolveAppealResult(ResolveAppealResponse? Response, bool ProfileMissing)
{
    public static ResolveAppealResult MissingProfile() => new(null, ProfileMissing: true);

    public static ResolveAppealResult Success(ResolveAppealResponse response) =>
        new(response, ProfileMissing: false);
}

/// <summary>
/// Resolves an appeal (backbone §6.8, §11). The resolution is always audited; an
/// <c>upheld</c> outcome also lifts the profile's restriction or ban through the
/// Moderation contract (the same <c>clear</c> path admins use directly), so an upheld
/// appeal actually reinstates the member rather than only recording a note.
/// </summary>
internal sealed class ResolveAppealHandler(
    ICurrentUser currentUser,
    IProfileApi profileApi,
    IModerationApi moderationApi,
    AdminAuditWriter auditWriter)
{
    public async Task<ResolveAppealResult> HandleAsync(
        Guid appealId,
        ResolveAppealRequest request,
        CancellationToken cancellationToken)
    {
        var targetUserId = await profileApi.GetUserIdForProfileAsync(request.TargetProfileId, cancellationToken);
        if (targetUserId is null)
        {
            return ResolveAppealResult.MissingProfile();
        }

        var outcome = request.Outcome.Trim().ToLowerInvariant();
        var upheld = outcome == "upheld";
        var adminUserId = currentUser.RequireUserId();

        // Audit before acting, so an upheld appeal that clears a restriction is never
        // left unaudited (§11).
        var audit = await auditWriter.WriteAsync(
            adminUserId,
            "appeal.resolve",
            $"appeal:{appealId}",
            new
            {
                appealId,
                targetProfileId = request.TargetProfileId,
                targetUserId = targetUserId.Value,
                outcome,
                request.Note,
            },
            cancellationToken);

        if (upheld)
        {
            await moderationApi.ApplyAdminProfileActionAsync(
                request.TargetProfileId,
                targetUserId.Value,
                ModerationActions.Clear,
                $"Appeal upheld: {request.Note}",
                adminUserId,
                cancellationToken);
        }

        return ResolveAppealResult.Success(new ResolveAppealResponse(
            audit.Id,
            appealId,
            request.TargetProfileId,
            outcome,
            RestrictionLifted: upheld));
    }
}
