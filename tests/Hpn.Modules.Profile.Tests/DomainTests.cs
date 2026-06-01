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
        profile.Status.Should().Be(ProfileStatus.Draft);
        profile.VisibilityPreferences.ShowOnlyOutsideCountry.Should().BeFalse();
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
}
