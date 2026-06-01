using FluentAssertions;
using Hpn.Modules.Identity.Internal.Domain;
using Xunit;

namespace Hpn.Modules.Identity.Tests;

public sealed class UserTests
{
    [Fact]
    public void Register_normalizes_email_and_sets_member_defaults()
    {
        var now = DateTimeOffset.UtcNow;

        var user = User.Register("  Person@Example.COM ", now);

        user.Email.Should().Be("person@example.com");
        user.Role.Should().Be(UserRole.Member);
        user.Status.Should().Be(UserStatus.Active);
        user.IsActive.Should().BeTrue();
        user.LastLoginAt.Should().BeNull();
        user.Id.Should().NotBe(Guid.Empty);
    }
}

public sealed class MagicLinkTokenTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Fresh_token_is_redeemable_within_its_lifetime()
    {
        var token = MagicLinkToken.Issue(Guid.NewGuid(), "hash", Now, TimeSpan.FromMinutes(15), requestedIp: null);

        token.IsRedeemable(Now.AddMinutes(5)).Should().BeTrue();
    }

    [Fact]
    public void Expired_token_is_not_redeemable()
    {
        var token = MagicLinkToken.Issue(Guid.NewGuid(), "hash", Now, TimeSpan.FromMinutes(15), requestedIp: null);

        token.IsRedeemable(Now.AddMinutes(16)).Should().BeFalse();
    }

    [Fact]
    public void Consumed_token_is_not_redeemable_again()
    {
        var token = MagicLinkToken.Issue(Guid.NewGuid(), "hash", Now, TimeSpan.FromMinutes(15), requestedIp: null);

        token.Consume(Now.AddMinutes(1));

        token.IsRedeemable(Now.AddMinutes(2)).Should().BeFalse();
        token.ConsumedAt.Should().Be(Now.AddMinutes(1));
    }
}

public sealed class SessionTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(30);

    [Fact]
    public void Active_until_expiry_then_inactive()
    {
        var session = Session.Start(Guid.NewGuid(), "hash", Now, Lifetime, userAgent: null, ip: null);

        session.IsActive(Now.AddDays(1)).Should().BeTrue();
        session.IsActive(Now.AddDays(31)).Should().BeFalse();
    }

    [Fact]
    public void Revoke_makes_it_inactive_and_is_idempotent()
    {
        var session = Session.Start(Guid.NewGuid(), "hash", Now, Lifetime, userAgent: null, ip: null);

        session.Revoke(Now.AddDays(1));
        var firstRevokedAt = session.RevokedAt;
        session.Revoke(Now.AddDays(2));

        session.IsActive(Now.AddDays(1)).Should().BeFalse();
        session.RevokedAt.Should().Be(firstRevokedAt);
    }

    [Fact]
    public void Slides_only_past_the_halfway_point()
    {
        var session = Session.Start(Guid.NewGuid(), "hash", Now, Lifetime, userAgent: null, ip: null);

        session.SlideIfDue(Now.AddDays(5), Lifetime).Should().BeFalse("still in the first half of the window");

        var slid = session.SlideIfDue(Now.AddDays(20), Lifetime);

        slid.Should().BeTrue();
        session.ExpiresAt.Should().Be(Now.AddDays(20) + Lifetime);
    }
}
