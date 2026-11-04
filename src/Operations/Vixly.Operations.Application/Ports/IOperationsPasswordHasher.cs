namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsPasswordHasher.cs
//
// PURPOSE:
// Hashes and checks staff passwords.
//
// RESPONSIBILITIES:
// - Hash a new password
// - Check a password
//
// IMPORTANT NOTES:
// - BCrypt cost 12 in Infrastructure (own code, ADR-0020 rejected)
// =========================================================
public interface IOperationsPasswordHasher
{
    string Hash(string password);

    OperationsPasswordCheck Verify(string password, string passwordHash);
}
