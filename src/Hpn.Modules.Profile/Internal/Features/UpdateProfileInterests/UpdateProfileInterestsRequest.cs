namespace Hpn.Modules.Profile.Internal.Features.UpdateProfileInterests;

internal sealed record UpdateProfileInterestsRequest(IReadOnlyCollection<Guid> InterestIds);
