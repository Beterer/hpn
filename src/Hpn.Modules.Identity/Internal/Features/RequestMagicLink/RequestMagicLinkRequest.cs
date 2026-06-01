namespace Hpn.Modules.Identity.Internal.Features.RequestMagicLink;

/// <summary>Body of <c>POST /auth/magic-link</c>.</summary>
internal sealed record RequestMagicLinkRequest(string Email);
