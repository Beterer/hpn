using System.Buffers.Text;
using System.Security.Cryptography;

namespace Hpn.Modules.Identity.Internal.Security;

/// <summary>
/// Produces high-entropy, URL-safe secrets for magic links and session cookies
/// (backbone §10.1). 256 bits of CSPRNG output, base64url-encoded without padding.
/// </summary>
internal static class SecureTokenGenerator
{
    private const int TokenBytes = 32;

    public static string Generate() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(TokenBytes));
}
