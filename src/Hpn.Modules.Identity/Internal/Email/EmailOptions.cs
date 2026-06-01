namespace Hpn.Modules.Identity.Internal.Email;

/// <summary>
/// Email delivery configuration bound from the <c>Email</c> section. Provider
/// selection is config-driven so dev (Mailpit/SMTP) and prod (Resend) swap with
/// no code change (backbone §10.9, §16.2). Secrets stay env-injected.
/// </summary>
internal sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary><c>smtp</c> (Mailpit/dev) or <c>resend</c> (prod).</summary>
    public string Provider { get; set; } = "smtp";

    public string FromAddress { get; set; } = "no-reply@notice.app";
    public string FromName { get; set; } = "Notice";

    public SmtpOptions Smtp { get; set; } = new();
    public ResendOptions Resend { get; set; } = new();

    internal sealed class SmtpOptions
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 1025;
    }

    internal sealed class ResendOptions
    {
        public string ApiKey { get; set; } = string.Empty;
    }
}
