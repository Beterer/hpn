namespace Hpn.Modules.Profile.Internal.Features.UpdateLocation;

/// <summary>
/// Sets coarse location with explicit consent (§10.4). With <c>Consent=false</c>
/// (or missing coordinates) any stored point is cleared.
/// </summary>
internal sealed record UpdateLocationRequest(bool Consent, double? Latitude, double? Longitude);

internal sealed record LocationResponse(bool Consent, bool HasLocation);
