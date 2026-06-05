namespace Hpn.Modules.Feed.Internal.ReadModel;

// The Feed module owns no tables (backbone §7.6). It is a sanctioned cross-schema
// READ model (§3.1): these lightweight, read-only row types map onto tables that
// other modules own and migrate. Feed never writes them and excludes them from its
// own migrations — ownership stays with the writing module; only the SELECT side
// is shared, and only here where it is visible in review.

internal sealed class FeedProfileRow
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string DisplayName { get; init; } = null!;
    public string Gender { get; init; } = null!;
    public string? SelfDescribeText { get; init; }
    // Internal-only (ADR-028): read for the same-country eligibility rule, never projected onto a card.
    public string? CountryCode { get; init; }
    public double? GeoLat { get; init; }
    public double? GeoLng { get; init; }
    public bool Verified { get; init; }
    public string Status { get; init; } = null!;
}

internal sealed class FeedVisibilityRow
{
    public Guid ProfileId { get; init; }
    public bool HideFromCountry { get; init; }
    public int? MinDistanceKm { get; init; }
    public bool WomenForWomen { get; init; }
    public bool VerifiedOnly { get; init; }
    public bool Paused { get; init; }
    public bool HiddenFromGuests { get; init; }
}

internal sealed class FeedBlockRow
{
    public Guid BlockerUserId { get; init; }
    public Guid BlockedUserId { get; init; }
}

internal sealed class FeedPhotoRow
{
    public Guid Id { get; init; }
    public Guid ProfileId { get; init; }
    public short Position { get; init; }
    public string Status { get; init; } = null!;
    public int Width { get; init; }
    public int Height { get; init; }
}

internal sealed class FeedAppreciationRow
{
    public Guid Id { get; init; }
    public Guid SenderUserId { get; init; }
    public Guid ReceiverProfileId { get; init; }
}

// profile.profile_interests (join) + profile.interests (catalog). Interests are
// already public (they appear on PublicProfileResponse), so surfacing them on the
// feed card is consistent with the privacy posture — read-only, like the rest.
internal sealed class FeedProfileInterestRow
{
    public Guid ProfileId { get; init; }
    public Guid InterestId { get; init; }
}

internal sealed class FeedInterestRow
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = null!;
    public string Label { get; init; } = null!;
}
