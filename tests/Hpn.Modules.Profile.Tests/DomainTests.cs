using FluentAssertions;
using Hpn.Modules.Profile.Internal.Domain;
using Xunit;

namespace Hpn.Modules.Profile.Tests;

public sealed class DomainTests
{
    [Fact]
    public void Create_starts_draft_with_safe_visibility_defaults()
    {
        var now = DateTimeOffset.UtcNow;
        var profile = UserProfile.Create(
            Guid.CreateVersion7(),
            "  Rowan  ",
            Gender.SelfDescribe,
            "  genderqueer  ",
            " ro ",
            "  Notices ordinary kindness. ",
            now);

        profile.DisplayName.Should().Be("Rowan");
        profile.Gender.Should().Be(Gender.SelfDescribe);
        profile.SelfDescribeText.Should().Be("genderqueer");
        profile.CountryCode.Should().Be("RO");
        profile.Bio.Should().Be("Notices ordinary kindness.");
        profile.Verified.Should().BeFalse();
        profile.Status.Should().Be(ProfileStatus.Draft);
        profile.VisibilityPreferences.HideFromCountry.Should().BeFalse();
        profile.VisibilityPreferences.WomenForWomen.Should().BeFalse();
        profile.VisibilityPreferences.VerifiedOnly.Should().BeFalse();
        profile.VisibilityPreferences.Paused.Should().BeFalse();
    }

    [Fact]
    public void Updating_to_fixed_gender_clears_self_describe_text()
    {
        var now = DateTimeOffset.UtcNow;
        var profile = UserProfile.Create(
            Guid.CreateVersion7(),
            "Rowan",
            Gender.SelfDescribe,
            "genderqueer",
            "RO",
            null,
            now);

        profile.UpdateDetails("Rowan", Gender.Woman, "should disappear", "RO", null, now.AddMinutes(1));

        profile.Gender.Should().Be(Gender.Woman);
        profile.SelfDescribeText.Should().BeNull();
    }

    [Fact]
    public void Status_lifecycle_runs_draft_to_active_to_paused()
    {
        var now = DateTimeOffset.UtcNow;
        var profile = UserProfile.Create(Guid.CreateVersion7(), "Rowan", Gender.Woman, null, "RO", null, now);

        profile.Pause(now).Should().BeFalse();
        profile.Status.Should().Be(ProfileStatus.Draft);

        profile.Activate(now.AddMinutes(1)).Should().BeTrue();
        profile.Status.Should().Be(ProfileStatus.Active);
        profile.VisibilityPreferences.Paused.Should().BeFalse();

        profile.Pause(now.AddMinutes(2)).Should().BeTrue();
        profile.Status.Should().Be(ProfileStatus.Paused);
        profile.VisibilityPreferences.Paused.Should().BeTrue();
    }

    [Fact]
    public void Set_location_rounds_to_a_coarse_grid_with_consent()
    {
        var now = DateTimeOffset.UtcNow;
        var profile = UserProfile.Create(Guid.CreateVersion7(), "Rowan", Gender.Woman, null, "RO", null, now);

        profile.SetLocation(44.4267, 26.1025, consent: true, now.AddMinutes(1));

        profile.LocationConsent.Should().BeTrue();
        profile.GeoLat.Should().Be(44.4);   // rounded to 0.1° (~11 km), never the precise point
        profile.GeoLng.Should().Be(26.1);
    }

    [Fact]
    public void Set_location_without_consent_clears_any_stored_point()
    {
        var now = DateTimeOffset.UtcNow;
        var profile = UserProfile.Create(Guid.CreateVersion7(), "Rowan", Gender.Woman, null, "RO", null, now);
        profile.SetLocation(44.4, 26.1, consent: true, now);

        profile.SetLocation(44.4, 26.1, consent: false, now.AddMinutes(1));

        profile.LocationConsent.Should().BeFalse();
        profile.GeoLat.Should().BeNull();
        profile.GeoLng.Should().BeNull();
    }

    [Fact]
    public void Mark_deleted_takes_the_profile_out_of_the_feed()
    {
        var now = DateTimeOffset.UtcNow;
        var profile = UserProfile.Create(Guid.CreateVersion7(), "Rowan", Gender.Woman, null, "RO", null, now);
        profile.Activate(now);

        profile.MarkDeleted(now.AddMinutes(1));

        profile.Status.Should().Be(ProfileStatus.Deleted);
        // IsVisibleTo requires active status, so a deleted profile is hidden from others.
        profile.IsVisibleTo(Guid.NewGuid(), hasBlockBetweenUsers: false).Should().BeFalse();
    }

    [Fact]
    public void Restrict_then_clear_returns_an_active_profile_to_the_feed()
    {
        var now = DateTimeOffset.UtcNow;
        var profile = UserProfile.Create(Guid.CreateVersion7(), "Rowan", Gender.Woman, null, "RO", null, now);
        profile.Activate(now);

        profile.Restrict(now.AddMinutes(1)).Should().BeTrue();
        profile.Status.Should().Be(ProfileStatus.UnderReview);
        profile.IsVisibleTo(Guid.NewGuid(), hasBlockBetweenUsers: false).Should().BeFalse();

        profile.ClearModeration(now.AddHours(48)).Should().BeTrue();
        profile.Status.Should().Be(ProfileStatus.Active);
        profile.IsVisibleTo(Guid.NewGuid(), hasBlockBetweenUsers: false).Should().BeTrue();
    }

    [Fact]
    public void Clearing_a_restriction_honours_a_self_pause_set_beforehand()
    {
        var now = DateTimeOffset.UtcNow;
        var profile = UserProfile.Create(Guid.CreateVersion7(), "Rowan", Gender.Woman, null, "RO", null, now);
        profile.Activate(now);
        profile.Pause(now.AddMinutes(1)); // the member took a break first

        profile.Restrict(now.AddMinutes(2)).Should().BeTrue();
        profile.Status.Should().Be(ProfileStatus.UnderReview);

        profile.ClearModeration(now.AddHours(48)).Should().BeTrue();
        // Restored to their own pause, not forced back into the feed.
        profile.Status.Should().Be(ProfileStatus.Paused);
        profile.VisibilityPreferences.Paused.Should().BeTrue();
    }

    [Fact]
    public void Moderation_never_resurrects_a_deleted_account()
    {
        var now = DateTimeOffset.UtcNow;
        var profile = UserProfile.Create(Guid.CreateVersion7(), "Rowan", Gender.Woman, null, "RO", null, now);
        profile.Activate(now);
        profile.MarkDeleted(now.AddMinutes(1));

        profile.Restrict(now.AddMinutes(2)).Should().BeFalse();
        profile.Ban(now.AddMinutes(3)).Should().BeFalse();
        profile.ClearModeration(now.AddMinutes(4)).Should().BeFalse();
        profile.Status.Should().Be(ProfileStatus.Deleted);
    }
}
