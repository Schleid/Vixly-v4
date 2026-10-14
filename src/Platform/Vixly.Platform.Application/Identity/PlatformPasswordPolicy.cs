using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.Identity;

// =========================================================
// FILE: PlatformPasswordPolicy.cs
//
// PURPOSE:
// The staff password rules (ADR-0006 section 4).
//
// RESPONSIBILITIES:
// - 12 to 64 characters
// - Not a well-known password, not the email or its name part
//
// IMPORTANT NOTES:
// - No composition rules (upper case, symbols): length beats
//   them, as NIST SP 800-63B advises
// - 64 characters keeps passwords inside BCrypt's 72-byte
//   limit for most scripts
// - The short list here blocks the worst offenders; a
//   breached-password service can be added behind the same
//   method later
// =========================================================
public static class PlatformPasswordPolicy
{
    public const int MinLength = 12;

    public const int MaxLength = 64;

    private static readonly HashSet<string> Common = new(StringComparer.OrdinalIgnoreCase)
    {
        "123456789012", "1234567890123", "12345678910111213", "qwertyuiopas", "qwerty123456",
        "password1234", "password12345", "passwordpassword", "P@ssw0rd1234", "Welcome12345",
        "letmein12345", "iloveyou1234", "administrator", "changeme1234", "abcdefghijkl",
        "aaaaaaaaaaaa", "111111111111", "000000000000", "vixlyvixly12", "Vixly2026!!!",
        "Galeo2026!!!", "techcrafters", "summer202626", "winter202626", "football1234"
    };

    public static Error? Check(string? password, string? email)
    {
        if (password is null || password.Length < MinLength)
        {
            return PlatformAuthErrors.PasswordTooShort;
        }

        if (password.Length > MaxLength)
        {
            return PlatformAuthErrors.PasswordTooLong;
        }

        if (Common.Contains(password) || password.Distinct().Count() <= 2)
        {
            return PlatformAuthErrors.PasswordTooCommon;
        }

        if (!string.IsNullOrEmpty(email))
        {
            string local = email.Split('@')[0];

            if (password.Contains(email, StringComparison.OrdinalIgnoreCase)
                || (local.Length >= 4 && password.Contains(local, StringComparison.OrdinalIgnoreCase)))
            {
                return PlatformAuthErrors.PasswordContainsEmail;
            }
        }

        return null;
    }
}
