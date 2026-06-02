namespace Hpn.Modules.Identity.Internal.Domain;

/// <summary>
/// An account. Identity owns the canonical user row; other modules reference it
/// by <see cref="Id"/> only (no cross-schema FKs — backbone §7, §6.1). There are
/// no passwords in v1 (magic-link only, §11).
/// </summary>
internal sealed class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public UserStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }
    public DateTimeOffset? DeletionRequestedAt { get; private set; }
    public DateTimeOffset? PurgeAfter { get; private set; }
    // The account's profile id, captured at soft-delete. The hard purge reads it
    // from here rather than re-resolving it live, so a retry after a partial purge
    // (where the profile row may already be gone) can still erase every module's
    // profile-keyed data (§10.5).
    public Guid? PendingDeletionProfileId { get; private set; }

    private User()
    {
    }

    public static User Register(string email, DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(),
        Email = email.Trim().ToLowerInvariant(),
        Role = UserRole.Member,
        Status = UserStatus.Active,
        CreatedAt = now,
    };

    public bool IsActive => Status == UserStatus.Active;

    public void RecordLogin(DateTimeOffset now) => LastLoginAt = now;

    /// <summary>
    /// Soft-delete (§10.5): the account is marked for deletion and a purge deadline
    /// is set. Sessions are revoked separately so access stops at once; the rows are
    /// only erased once <paramref name="now"/> passes <see cref="PurgeAfter"/>.
    /// </summary>
    public void RequestDeletion(DateTimeOffset now, TimeSpan graceWindow, Guid? profileId)
    {
        Status = UserStatus.PendingDeletion;
        DeletionRequestedAt = now;
        PurgeAfter = now + graceWindow;
        PendingDeletionProfileId = profileId;
    }
}
