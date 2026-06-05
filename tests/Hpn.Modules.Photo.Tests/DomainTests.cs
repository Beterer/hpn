using FluentAssertions;
using Hpn.Modules.Photo.Internal.Domain;
using Xunit;

namespace Hpn.Modules.Photo.Tests;

public sealed class DomainTests
{
    [Fact]
    public void Create_ready_photo_sets_primary_flag_and_metadata()
    {
        var now = DateTimeOffset.UtcNow;
        var photo = ProfilePhoto.CreateReady(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            position: 0,
            originalKey: "profiles/p/photos/x/original.webp",
            displayKey: "profiles/p/photos/x/display.webp",
            thumbKey: "profiles/p/photos/x/thumb.webp",
            width: 1200,
            height: 900,
            contentHash: new string('a', 64),
            scanResult: "pass",
            now,
            isPrimary: true);

        photo.Position.Should().Be(0);
        photo.IsPrimary.Should().BeTrue();
        photo.Status.Should().Be(PhotoStatus.Ready);
        photo.ScanResult.Should().Be("pass");
        photo.CreatedAt.Should().Be(now);
    }

    [Fact]
    public void Setting_primary_does_not_change_position()
    {
        var photo = ProfilePhoto.CreateReady(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            position: 4,
            originalKey: "original.webp",
            displayKey: "display.webp",
            thumbKey: "thumb.webp",
            width: 1,
            height: 1,
            contentHash: new string('c', 64),
            scanResult: null,
            DateTimeOffset.UtcNow);

        photo.SetPrimary(true);

        photo.IsPrimary.Should().BeTrue();
        photo.Position.Should().Be(4);
    }

    [Fact]
    public void Move_rejects_negative_position()
    {
        var photo = ProfilePhoto.CreateReady(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            position: 0,
            originalKey: "original.webp",
            displayKey: "display.webp",
            thumbKey: "thumb.webp",
            width: 1,
            height: 1,
            contentHash: new string('b', 64),
            scanResult: null,
            DateTimeOffset.UtcNow);

        var act = () => photo.MoveTo(-1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
