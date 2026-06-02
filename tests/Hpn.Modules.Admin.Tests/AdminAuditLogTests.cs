using FluentAssertions;
using Hpn.Modules.Admin.Internal.Domain;
using Xunit;

namespace Hpn.Modules.Admin.Tests;

public sealed class AdminAuditLogTests
{
    [Fact]
    public void Record_trims_decision_fields_and_defaults_empty_metadata()
    {
        var adminUserId = Guid.NewGuid();
        var now = DateTimeOffset.Parse("2026-06-02T12:00:00Z");

        var audit = AdminAuditLog.Record(
            adminUserId,
            " profile_action:ban ",
            " profile:abc ",
            "  ",
            now);

        audit.Id.Should().NotBeEmpty();
        audit.AdminUserId.Should().Be(adminUserId);
        audit.Action.Should().Be("profile_action:ban");
        audit.TargetRef.Should().Be("profile:abc");
        audit.Metadata.Should().Be("{}");
        audit.CreatedAt.Should().Be(now);
    }
}
