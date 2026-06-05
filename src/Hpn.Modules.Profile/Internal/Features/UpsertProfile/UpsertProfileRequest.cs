namespace Hpn.Modules.Profile.Internal.Features.UpsertProfile;

internal sealed record UpsertProfileRequest(
    string DisplayName,
    string Gender,
    string? SelfDescribeText);
