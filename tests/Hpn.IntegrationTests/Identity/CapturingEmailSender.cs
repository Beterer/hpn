using System.Collections.Concurrent;
using Hpn.Modules.Identity.Internal.Email;

namespace Hpn.IntegrationTests.Identity;

/// <summary>
/// Test double for <see cref="IEmailSender"/> that records the magic links it
/// would have sent, so a test can extract the token and complete the flow
/// without a real mailbox. Visible because Identity grants
/// <c>InternalsVisibleTo("Hpn.IntegrationTests")</c>.
/// </summary>
internal sealed class CapturingEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<(string Email, string Url)> _sent = new();

    public IReadOnlyCollection<(string Email, string Url)> Sent => _sent;

    public Task SendMagicLinkAsync(string toEmail, string magicLinkUrl, CancellationToken cancellationToken = default)
    {
        _sent.Enqueue((toEmail, magicLinkUrl));
        return Task.CompletedTask;
    }

    public string? LastTokenFor(string email)
    {
        var url = _sent.Where(x => x.Email == email).Select(x => x.Url).LastOrDefault();
        if (url is null)
        {
            return null;
        }

        var marker = "token=";
        var index = url.IndexOf(marker, StringComparison.Ordinal);
        return index < 0 ? null : Uri.UnescapeDataString(url[(index + marker.Length)..]);
    }
}
