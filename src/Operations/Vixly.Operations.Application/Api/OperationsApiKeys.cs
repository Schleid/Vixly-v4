using System.Security.Cryptography;
using System.Text;

namespace Vixly.Operations.Application.Api;

// =========================================================
// FILE: OperationsApiKeys.cs
//
// PURPOSE:
// Makes and reads company API keys (Slice 10).
//
// RESPONSIBILITIES:
// - Make a key: a public id and a random secret, shown once as one token
// - Read a token back into its id and secret; hash a secret; compare in constant time
//
// IMPORTANT NOTES:
// - Token: ak_<16 hex id>_<43 characters>; only the SHA-256 of the secret is kept
// =========================================================
public static class OperationsApiKeys
{
    public const string Prefix = "ak_";

    public static (string KeyId, string Token, string SecretHash, string Hint) Generate()
    {
        string keyId = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(8));
        string secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return (keyId, Prefix + keyId + "_" + secret, Hash(secret), secret[^4..]);
    }

    public static bool TryRead(string? token, out string keyId, out string secret)
    {
        (keyId, secret) = (string.Empty, string.Empty);

        if (token is null || token.Length is < 30 or > 120 || !token.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        string rest = token[Prefix.Length..];
        int split = rest.IndexOf('_', StringComparison.Ordinal);

        if (split != 16 || !rest[..16].All(char.IsAsciiHexDigitLower) || rest.Length - 17 < 20)
        {
            return false;
        }

        (keyId, secret) = (rest[..16], rest[17..]);
        return true;
    }

    public static string Hash(string secret) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(secret ?? throw new ArgumentNullException(nameof(secret)))));

    public static bool Matches(string secret, string secretHash) =>
        CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(Hash(secret)), Encoding.ASCII.GetBytes(secretHash ?? string.Empty));
}
