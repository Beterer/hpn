namespace Hpn.Modules.Admin.Internal.Domain;

/// <summary>
/// Audit trail for internal admin decisions (backbone §7.9, §11). The Admin module
/// owns this schema; decisions in other modules are still executed through their
/// public contracts and mirrored here for operator accountability.
/// </summary>
internal sealed class AdminAuditLog
{
    public Guid Id { get; private set; }
    public Guid AdminUserId { get; private set; }
    public string Action { get; private set; } = null!;
    public string TargetRef { get; private set; } = null!;
    public string Metadata { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    private AdminAuditLog()
    {
    }

    public static AdminAuditLog Record(
        Guid adminUserId,
        string action,
        string targetRef,
        string metadata,
        DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(),
        AdminUserId = adminUserId,
        Action = action.Trim(),
        TargetRef = targetRef.Trim(),
        Metadata = string.IsNullOrWhiteSpace(metadata) ? "{}" : metadata,
        CreatedAt = now,
    };
}
