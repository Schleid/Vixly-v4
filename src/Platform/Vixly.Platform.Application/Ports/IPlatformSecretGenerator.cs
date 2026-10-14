namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformSecretGenerator.cs
//
// PURPOSE:
// Generates the random secrets the identity model stores as
// hashes: session tokens, account-link tokens and recovery
// codes.
//
// RESPONSIBILITIES:
// - Create a URL-safe token with 256 bits of randomness
// - Create a human-typeable recovery code
// - Hash any of them the same way the database expects
//
// IMPORTANT NOTES:
// - Hashes are lowercase hex SHA-256 (PlatformTokenHash)
// - Recovery codes are normalised (case, spaces, dashes)
//   before hashing, so the user may type them loosely
// =========================================================
public interface IPlatformSecretGenerator
{
    PlatformSecret NewToken();

    PlatformSecret NewRecoveryCode();

    string HashToken(string token);

    string HashRecoveryCode(string code);
}
