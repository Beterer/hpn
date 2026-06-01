namespace Hpn.Modules.Identity.Internal.Features.VerifyMagicLink;

/// <summary>Body of <c>POST /auth/verify</c> — the raw token from the emailed link.</summary>
internal sealed record VerifyMagicLinkRequest(string Token);
