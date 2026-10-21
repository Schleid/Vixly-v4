using System.Security.Cryptography;
using System.Text;
using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Infrastructure.Security;

// =========================================================
// FILE: OperationsSecretGenerator.cs
//
// PURPOSE:
// Random tokens and recovery codes, hashed with SHA-256.
//
// RESPONSIBILITIES:
// - Make 256-bit URL-safe tokens
// - Make XXXXX-XXXXX recovery codes without look-alike characters
//
// IMPORTANT NOTES:
// - Recovery codes are compared without case, spaces or dashes
// =========================================================
public sealed class OperationsSecretGenerator : IOperationsSecretGenerator
{
    private const string RecoveryAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public OperationsSecret NewToken()
    {
        string token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return new OperationsSecret(token, HashToken(token));
    }

    public OperationsSecret NewRecoveryCode()
    {
        char[] characters = new char[10];

        for (int index = 0; index < characters.Length; index++)
        {
            characters[index] = RecoveryAlphabet[RandomNumberGenerator.GetInt32(RecoveryAlphabet.Length)];
        }

        string code = new string(characters, 0, 5) + "-" + new string(characters, 5, 5);
        return new OperationsSecret(code, HashRecoveryCode(code));
    }

    public string HashToken(string token)
    {
        ArgumentNullException.ThrowIfNull(token);
        return Sha256Hex(token);
    }

    public string HashRecoveryCode(string code)
    {
        ArgumentNullException.ThrowIfNull(code);
        return Sha256Hex(new string(code.Where(character => !char.IsWhiteSpace(character) && character != '-').Select(char.ToUpperInvariant).ToArray()));
    }

    private static string Sha256Hex(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
