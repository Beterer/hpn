namespace Hpn.Modules.Identity.Contracts.Dtos;

/// <summary>Minimal account projection other modules may read (backbone §6.1).</summary>
public sealed record IdentityUserDto(Guid Id, string Email, string Role);
