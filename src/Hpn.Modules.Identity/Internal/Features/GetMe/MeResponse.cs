namespace Hpn.Modules.Identity.Internal.Features.GetMe;

/// <summary>
/// Onboarding progress surfaced to the SPA so it can route a fresh user to the
/// right step (backbone §9.2). In M1 the only fact Identity owns is "you're
/// authenticated"; later milestones fill these from the Profile/Photo contracts.
/// </summary>
internal sealed record OnboardingDto(bool ProfileCreated, bool ProfileActive, string NextStep);

internal sealed record MeResponse(Features.AuthUserDto User, OnboardingDto Onboarding);
