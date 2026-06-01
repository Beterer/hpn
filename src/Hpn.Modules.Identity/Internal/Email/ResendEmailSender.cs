using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace Hpn.Modules.Identity.Internal.Email;

/// <summary>
/// Production sender via Resend's HTTP API (backbone §16.2). The API key is
/// env-injected (§10.9); behind <see cref="IEmailSender"/> so SES or another
/// provider can replace it without touching callers.
/// </summary>
internal sealed class ResendEmailSender(HttpClient httpClient, IOptions<EmailOptions> options) : IEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendMagicLinkAsync(string toEmail, string magicLinkUrl, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            from = $"{_options.FromName} <{_options.FromAddress}>",
            to = new[] { toEmail },
            subject = MagicLinkEmailContent.Subject,
            html = MagicLinkEmailContent.Html(magicLinkUrl),
            text = MagicLinkEmailContent.Text(magicLinkUrl),
        };

        using var response = await httpClient.PostAsJsonAsync("emails", payload, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
