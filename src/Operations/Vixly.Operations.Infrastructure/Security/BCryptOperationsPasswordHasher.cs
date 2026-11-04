using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Infrastructure.Security;

// =========================================================
// FILE: BCryptOperationsPasswordHasher.cs
//
// PURPOSE:
// BCrypt password hashing for staff (cost 12, upgraded on sign-in, M-4).
//
// RESPONSIBILITIES:
// - Hash and verify
// - Report hashes made with a lower cost
//
// IMPORTANT NOTES:
// - Own code (ADR-0020 rejected); same rules as Platform
// =========================================================
public sealed class BCryptOperationsPasswordHasher : IOperationsPasswordHasher
{
    public const int WorkFactor = 12;

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);
        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    public OperationsPasswordCheck Verify(string password, string passwordHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(passwordHash))
        {
            return OperationsPasswordCheck.Failed;
        }

        try
        {
            if (!BCrypt.Net.BCrypt.Verify(password, passwordHash))
            {
                return OperationsPasswordCheck.Failed;
            }

            return BCrypt.Net.BCrypt.PasswordNeedsRehash(passwordHash, WorkFactor)
                ? OperationsPasswordCheck.SucceededNeedsRehash
                : OperationsPasswordCheck.Succeeded;
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return OperationsPasswordCheck.Failed;
        }
    }
}
