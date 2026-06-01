using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Hpn.Modules.Identity.Internal.Email;

/// <summary>
/// SMTP sender for local development — points at Mailpit, which captures every
/// message so magic links are visible without a real mailbox (backbone §13.1).
/// Uses the BCL <see cref="SmtpClient"/> to avoid pulling in an extra dependency
/// for the dev-only path.
/// </summary>
internal sealed class SmtpEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendMagicLinkAsync(string toEmail, string magicLinkUrl, CancellationToken cancellationToken = default)
    {
        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = MagicLinkEmailContent.Subject,
            Body = MagicLinkEmailContent.Html(magicLinkUrl),
            IsBodyHtml = true,
        };
        message.To.Add(toEmail);

        using var client = new SmtpClient(_options.Smtp.Host, _options.Smtp.Port);
        await client.SendMailAsync(message, cancellationToken);
    }
}
