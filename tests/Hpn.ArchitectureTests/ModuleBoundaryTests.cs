using System.Reflection;
using FluentAssertions;
using Hpn.Modules.Admin;
using Hpn.Modules.Appreciation;
using Hpn.Modules.Feed;
using Hpn.Modules.Identity;
using Hpn.Modules.Moderation;
using Hpn.Modules.Notification;
using Hpn.Modules.Photo;
using Hpn.Modules.Profile;
using Hpn.Modules.SocialFingerprint;
using Microsoft.EntityFrameworkCore;
using NetArchTest.Rules;
using Xunit;

namespace Hpn.ArchitectureTests;

/// <summary>
/// CI backstop for the module boundary (backbone §5.2). The compiler already
/// blocks reaching another module's internals; these assertions keep the rule
/// visible and catch erosion via contracts/shared types.
/// </summary>
public sealed class ModuleBoundaryTests
{
    private static readonly IReadOnlyDictionary<string, Assembly> Modules = new Dictionary<string, Assembly>
    {
        ["Identity"] = typeof(IdentityModule).Assembly,
        ["Profile"] = typeof(ProfileModule).Assembly,
        ["Photo"] = typeof(PhotoModule).Assembly,
        ["Appreciation"] = typeof(AppreciationModule).Assembly,
        ["Feed"] = typeof(FeedModule).Assembly,
        ["SocialFingerprint"] = typeof(SocialFingerprintModule).Assembly,
        ["Moderation"] = typeof(ModerationModule).Assembly,
        ["Admin"] = typeof(AdminModule).Assembly,
        ["Notification"] = typeof(NotificationModule).Assembly,
    };

    [Fact]
    public void Modules_do_not_depend_on_another_modules_internals()
    {
        foreach (var (name, assembly) in Modules)
        {
            var forbidden = Modules.Keys
                .Where(other => other != name)
                .Select(other => $"Hpn.Modules.{other}.Internal")
                .ToArray();

            var result = Types.InAssembly(assembly)
                .Should()
                .NotHaveDependencyOnAny(forbidden)
                .GetResult();

            result.IsSuccessful.Should().BeTrue(
                $"{name} must not reference another module's Internal namespace. Offenders: " +
                string.Join(", ", result.FailingTypeNames ?? []));
        }
    }

    [Fact]
    public void DbContexts_are_internal_to_their_module()
    {
        foreach (var (name, assembly) in Modules)
        {
            var result = Types.InAssembly(assembly)
                .That()
                .Inherit(typeof(DbContext))
                .Should()
                .NotBePublic()
                .GetResult();

            result.IsSuccessful.Should().BeTrue(
                $"{name}'s DbContext must be internal. Offenders: " +
                string.Join(", ", result.FailingTypeNames ?? []));
        }
    }

    [Fact]
    public void Each_module_owns_exactly_one_DbContext()
    {
        foreach (var (name, assembly) in Modules)
        {
            var contexts = assembly.GetTypes()
                .Where(t => typeof(DbContext).IsAssignableFrom(t) && t != typeof(DbContext))
                .ToArray();

            contexts.Should().ContainSingle(
                $"{name} should declare exactly one DbContext (schema-per-module, no shared context)");
        }
    }
}
