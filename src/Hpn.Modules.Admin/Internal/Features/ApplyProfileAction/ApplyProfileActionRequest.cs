namespace Hpn.Modules.Admin.Internal.Features.ApplyProfileAction;

internal sealed record ApplyProfileActionRequest(
    string Action,
    string Reason);

internal sealed record ApplyProfileActionResponse(
    Guid AuditId,
    Guid TargetProfileId,
    Guid TargetUserId,
    string Action,
    DateTimeOffset? ExpiresAt);
