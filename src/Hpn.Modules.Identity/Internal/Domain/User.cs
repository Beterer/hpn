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
}
