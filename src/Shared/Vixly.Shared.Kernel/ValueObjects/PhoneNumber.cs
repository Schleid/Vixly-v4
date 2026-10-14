using System.Text;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Shared.Kernel.ValueObjects;

// =========================================================
// FILE: PhoneNumber.cs
//
// PURPOSE:
// A normalized phone number.
//
// RESPONSIBILITIES:
// - Keep digits and one leading plus sign, drop all other
//   formatting characters
// - Require between 7 and 15 digits
// - Provide value-based equality
//
// IMPORTANT NOTES:
// - A practical shape check, not telecom validation; it does
//   not prove the number exists or is reachable
// =========================================================
public sealed record PhoneNumber
{
    public const int MinimumDigits = 7;
    public const int MaximumDigits = 15;

    private PhoneNumber(string value)
    {
        Value = value;
    }

    public string Value { get; }

    // =====================================================
    // METHOD: CREATE
    // =====================================================
    public static PhoneNumber Create(string? value)
    {
        if (!TryCreate(value, out PhoneNumber? phone))
        {
            throw new DomainException(KernelErrors.PhoneInvalid);
        }

        return phone;
    }

    // =====================================================
    // METHOD: TRY CREATE
    // =====================================================
    public static bool TryCreate(
        string? value,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out PhoneNumber? phone)
    {
        phone = null;

        StringBuilder builder = new();

        foreach (char character in (value ?? string.Empty).Trim())
        {
            if (char.IsAsciiDigit(character))
            {
                builder.Append(character);
            }
            else if (character == '+' && builder.Length == 0)
            {
                builder.Append(character);
            }
        }

        string normalized = builder.ToString();
        int digitCount = normalized.Count(char.IsAsciiDigit);

        if (digitCount is < MinimumDigits or > MaximumDigits)
        {
            return false;
        }

        phone = new PhoneNumber(normalized);
        return true;
    }

    public override string ToString()
    {
        return Value;
    }
}
