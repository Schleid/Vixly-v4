using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.Identity;

// =========================================================
// FILE: OperationsPasswordPolicy.cs
//
// PURPOSE:
// The password rules for staff (Slice 1, as Platform).
//
// RESPONSIBILITIES:
// - 12 to 64 characters
// - Refuse common passwords and ones built from the email
//
// IMPORTANT NOTES:
// - Own copy of the rule (ADR-0020 rejected)
// =========================================================
public static class OperationsPasswordPolicy
{
    public const int MinLength = 12;

    public const int MaxLength = 64;

    private static readonly HashSet<string> Common = new(StringComparer.OrdinalIgnoreCase)
    {
        "123456789012", "1234567890123", "12345678910111213", "qwertyuiopas", "qwerty123456",
        "password1234", "password12345", "passwordpassword", "P@ssw0rd1234", "Welcome12345",
        "letmein12345", "iloveyou1234", "administrator", "changeme1234", "abcdefghijkl",
        "aaaaaaaaaaaa", "111111111111", "000000000000", "summer202626", "winter202626", "football1234"
    };

    public static Error? Check(string? password, string? email)
    {
        if (password is null || password.Length < MinLength)
        {
            return OperationsAuthErrors.PasswordTooShort;
        }

        if (password.Length > MaxLength)
        {
            return OperationsAuthErrors.PasswordTooLong;
        }

        if (Common.Contains(password) || password.Distinct().Count() <= 2)
        {
            return OperationsAuthErrors.PasswordTooCommon;
        }

        if (!string.IsNullOrEmpty(email))
        {
            string local = email.Split('@')[0];

            if (password.Contains(email, StringComparison.OrdinalIgnoreCase)
                || (local.Length >= 4 && password.Contains(local, StringComparison.OrdinalIgnoreCase)))
            {
                return OperationsAuthErrors.PasswordContainsEmail;
            }
        }

        return null;
    }
}
