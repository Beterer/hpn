namespace Hpn.Modules.Profile.Contracts.Dtos;

public sealed record PublicProfileDto(
    Guid Id,
    string DisplayName,
    string Gender,
    string? SelfDescribeText,
    string? CountryCode,
    string? Bio,
    bool Verified,
    IReadOnlyCollection<PublicInterestDto> Interests);
