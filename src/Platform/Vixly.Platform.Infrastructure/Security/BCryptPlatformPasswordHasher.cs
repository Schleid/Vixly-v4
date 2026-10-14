using Vixly.Platform.Application.Ports;

namespace Vixly.Platform.Infrastructure.Security;

// =========================================================
// FILE: BCryptPlatformPasswordHasher.cs
//
// PURPOSE:
// BCrypt password hashing for staff accounts.
//
// RESPONSIBILITIES:
// - Hash at cost 12 (ADR-0006)
// - Verify any BCrypt hash, including V3's lower-cost ones,
//   and ask for an upgrade when the cost is lower
//
// IMPORTANT NOTES:
// - A malformed hash counts as a failed check, never an error
//   shown to the user
// - BCrypt reads at most 72 bytes of a password; the 12 to
//   64 character rule in the use cases keeps passwords inside
//   that limit
// =========================================================
public sealed class BCryptPlatformPasswordHasher : IPlatformPasswordHasher
{
    public const int WorkFactor = 12;

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    public PlatformPasswordCheck Verify(string password, string passwordHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(passwordHash))
        {
            return PlatformPasswordCheck.Failed;
        }

        try
        {
            if (!BCrypt.Net.BCrypt.Verify(password, passwordHash))
            {
                return PlatformPasswordCheck.Failed;
            }

            return BCrypt.Net.BCrypt.PasswordNeedsRehash(passwordHash, WorkFactor)
                ? PlatformPasswordCheck.SucceededNeedsRehash
                : PlatformPasswordCheck.Succeeded;
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return PlatformPasswordCheck.Failed;
        }
    }
}
