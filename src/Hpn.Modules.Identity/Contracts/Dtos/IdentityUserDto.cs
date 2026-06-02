namespace Hpn.Modules.Identity.Contracts.Dtos;

/// <summary>Minimal account projection other modules may read (backbone §6.1).
/// <paramref name="CreatedAt"/> feeds the account-age input of the moderation
/// trust score (§10.3).</summary>
public sealed record IdentityUserDto(Guid Id, string Email, string Role, DateTimeOffset CreatedAt);
