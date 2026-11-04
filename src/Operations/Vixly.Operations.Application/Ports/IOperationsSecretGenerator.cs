namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsSecretGenerator.cs
//
// PURPOSE:
// Makes random tokens and recovery codes and hashes them.
//
// RESPONSIBILITIES:
// - Make tokens and recovery codes
// - Hash them the same way every time
//
// IMPORTANT NOTES:
// - SHA-256 hex hashes
// =========================================================
public interface IOperationsSecretGenerator
{
    OperationsSecret NewToken();

    OperationsSecret NewRecoveryCode();

    string HashToken(string token);

    string HashRecoveryCode(string code);
}
