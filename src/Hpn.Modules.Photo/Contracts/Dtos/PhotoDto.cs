namespace Hpn.Modules.Photo.Contracts.Dtos;

public sealed record PhotoDto(
    Guid Id,
    Guid ProfileId,
    int Position,
    bool IsPrimary,
    string Status,
    int Width,
    int Height,
    DateTimeOffset CreatedAt);
