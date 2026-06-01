namespace Hpn.Modules.Profile.Internal.Domain;

internal static class InterestSeed
{
    public static readonly IReadOnlyList<Interest> All =
    [
        new(new Guid("018f0b65-2c2a-7a10-9d4d-47b6f6a10001"), "books", "Books"),
        new(new Guid("018f0b65-2c2a-7a10-9d4d-47b6f6a10002"), "music", "Music"),
        new(new Guid("018f0b65-2c2a-7a10-9d4d-47b6f6a10003"), "art", "Art"),
        new(new Guid("018f0b65-2c2a-7a10-9d4d-47b6f6a10004"), "food", "Food"),
        new(new Guid("018f0b65-2c2a-7a10-9d4d-47b6f6a10005"), "nature", "Nature"),
        new(new Guid("018f0b65-2c2a-7a10-9d4d-47b6f6a10006"), "travel", "Travel"),
        new(new Guid("018f0b65-2c2a-7a10-9d4d-47b6f6a10007"), "movement", "Movement"),
        new(new Guid("018f0b65-2c2a-7a10-9d4d-47b6f6a10008"), "film", "Film"),
        new(new Guid("018f0b65-2c2a-7a10-9d4d-47b6f6a10009"), "technology", "Technology"),
        new(new Guid("018f0b65-2c2a-7a10-9d4d-47b6f6a1000a"), "learning", "Learning"),
        new(new Guid("018f0b65-2c2a-7a10-9d4d-47b6f6a1000b"), "volunteering", "Volunteering"),
        new(new Guid("018f0b65-2c2a-7a10-9d4d-47b6f6a1000c"), "craft", "Craft"),
    ];
}
