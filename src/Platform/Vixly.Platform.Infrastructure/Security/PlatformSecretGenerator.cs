using System.Security.Cryptography;
using System.Text;
using Vixly.Platform.Application.Ports;

namespace Vixly.Platform.Infrastructure.Security;

// =========================================================
// FILE: PlatformSecretGenerator.cs
//
// PURPOSE:
// Cryptographically random tokens and recovery codes, and
// their SHA-256 hashes.
//
// RESPONSIBILITIES:
// - Tokens: 32 random bytes, base64url
// - Recovery codes: 10 characters from an alphabet without
//   look-alikes, shown as XXXXX-XXXXX
// - Hash as lowercase hex SHA-256
//
// IMPORTANT NOTES:
// - Recovery codes are normalised before hashing: upper case,
//   no spaces or dashes
// =========================================================
public sealed class PlatformSecretGenerator : IPlatformSecretGenerator
{
    private const string RecoveryAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public PlatformSecret NewToken()
    {
        string token = Base64Url(RandomNumberGenerator.GetBytes(32));

        return new PlatformSecret(token, HashToken(token));
    }

    public PlatformSecret NewRecoveryCode()
    {
        char[] characters = new char[10];

        for (int index = 0; index < characters.Length; index++)
        {
            characters[index] = RecoveryAlphabet[RandomNumberGenerator.GetInt32(RecoveryAlphabet.Length)];
        }

        string code = new string(characters, 0, 5) + "-" + new string(characters, 5, 5);

        return new PlatformSecret(code, HashRecoveryCode(code));
    }

    public string HashToken(string token)
    {
        ArgumentNullException.ThrowIfNull(token);

        return Sha256Hex(token);
    }

    public string HashRecoveryCode(string code)
    {
        ArgumentNullException.ThrowIfNull(code);

        string normalised = new(code
            .Where(character => !char.IsWhiteSpace(character) && character != '-')
            .Select(char.ToUpperInvariant)
            .ToArray());

        return Sha256Hex(normalised);
    }

    private static string Sha256Hex(string value)
    {
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    private static string Base64Url(byte[] bytes)
    {
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
