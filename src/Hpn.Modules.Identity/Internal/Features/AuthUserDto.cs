namespace Hpn.Modules.Identity.Internal.Features;

/// <summary>The authenticated account as the SPA sees it (wire DTO, camelCased by the host).</summary>
internal sealed record AuthUserDto(Guid Id, string Email, string Role);
