using FluentAssertions;
using Hpn.Modules.Identity.Internal.Security;
using Xunit;

namespace Hpn.Modules.Identity.Tests;

public sealed class TokenHasherTests
{
    [Fact]
    public void Hash_is_deterministic_for_the_same_input()
    {
        const string raw = "a-secret-token-value";

        TokenHasher.Hash(raw).Should().Be(TokenHasher.Hash(raw));
    }

    [Fact]
    public void Hash_differs_for_different_inputs()
    {
        TokenHasher.Hash("token-one").Should().NotBe(TokenHasher.Hash("token-two"));
    }

    [Fact]
    public void Hash_does_not_reveal_the_raw_token()
    {
        const string raw = "another-secret";

        TokenHasher.Hash(raw).Should().NotContain(raw);
    }
}

public sealed class SecureTokenGeneratorTests
{
    [Fact]
    public void Generates_unique_url_safe_tokens()
    {
        var tokens = Enumerable.Range(0, 100).Select(_ => SecureTokenGenerator.Generate()).ToArray();

        tokens.Should().OnlyHaveUniqueItems();
        tokens.Should().AllSatisfy(t =>
        {
            t.Length.Should().BeGreaterThan(32);
            t.Should().NotContainAny("+", "/", "=");
        });
    }
}
