using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace Hpn.Modules.Identity.Internal.Security;

/// <summary>
/// One-way hashing for stored secrets. Magic-link and session tokens are looked
/// up by hash so the raw value is never persisted (backbone §10.1, §11). SHA-256
/// is sufficient here because the input is already 256 bits of CSPRNG entropy —
/// there is nothing to brute-force, unlike a password.
/// </summary>
internal static class TokenHasher
{
    public static string Hash(string rawToken)
    {
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Base64Url.EncodeToString(digest);
    }
}
