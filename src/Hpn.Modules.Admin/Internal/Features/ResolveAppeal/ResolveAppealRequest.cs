namespace Hpn.Modules.Admin.Internal.Features.ResolveAppeal;

/// <summary>
/// Resolves an appeal about a moderated profile (backbone §6.8). <c>Outcome</c> is
/// <c>upheld</c> (the appeal succeeds → the profile's restriction/ban is lifted) or
/// <c>dismissed</c> (the decision stands). <c>TargetProfileId</c> says which profile
/// the appeal concerns, so an upheld appeal can actually act, not just record.
/// </summary>
internal sealed record ResolveAppealRequest(
    Guid TargetProfileId,
    string Outcome,
    string Note);

internal sealed record ResolveAppealResponse(
    Guid AuditId,
    Guid AppealId,
    Guid TargetProfileId,
    string Outcome,
    bool RestrictionLifted);
