namespace Hpn.Modules.Identity.Internal.Email;

/// <summary>
/// The copy for the sign-in email, in one place so both senders stay identical.
/// Notice voice: warm, plain, never salesy (backbone §2).
/// </summary>
internal static class MagicLinkEmailContent
{
    public const string Subject = "Your Notice sign-in link";

    public static string Html(string magicLinkUrl) =>
        $"""
         <p>Hi,</p>
         <p>Tap the button below to sign in to Notice. The link works once and expires in about 15 minutes.</p>
         <p><a href="{magicLinkUrl}" style="display:inline-block;padding:12px 20px;background:#0f172a;color:#fff;border-radius:8px;text-decoration:none">Sign in to Notice</a></p>
         <p>If the button doesn't work, paste this link into your browser:<br><a href="{magicLinkUrl}">{magicLinkUrl}</a></p>
         <p>If you didn't ask to sign in, you can safely ignore this email.</p>
         """;

    public static string Text(string magicLinkUrl) =>
        $"""
         Hi,

         Use this link to sign in to Notice. It works once and expires in about 15 minutes:

         {magicLinkUrl}

         If you didn't ask to sign in, you can safely ignore this email.
         """;
}
