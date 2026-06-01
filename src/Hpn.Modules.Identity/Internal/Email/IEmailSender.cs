namespace Hpn.Modules.Identity.Internal.Email;

/// <summary>
/// Transactional email seam (backbone §M1, §16.2). Resend in prod, Mailpit (SMTP)
/// in dev — selected by configuration. Identity is the only sender in the MVP, so
/// the contract is intentionally narrow.
/// </summary>
internal interface IEmailSender
{
    Task SendMagicLinkAsync(string toEmail, string magicLinkUrl, CancellationToken cancellationToken = default);
}
