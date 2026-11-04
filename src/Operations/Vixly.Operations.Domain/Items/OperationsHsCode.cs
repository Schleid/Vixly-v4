using System.Globalization;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Items;

// =========================================================
// FILE: OperationsHsCode.cs
//
// PURPOSE:
// A customs (Harmonized System) code: 6 digits, or up to 10 with the national extension (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - Keep only its digits, between 6 and 10 of them
// - Show it the way customs papers do: 6404.11 or 6404.11.0000
//
// IMPORTANT NOTES:
// - Stored as digits only; the dots are for reading
// =========================================================
public static class OperationsHsCode
{
    public const int MinDigits = 6;

    public const int MaxDigits = 10;

    // Digits only (dots and spaces typed are dropped); empty gives null; anything else outside 6 to 10 digits is refused.
    public static string? Normalize(string? typed)
    {
        if (string.IsNullOrWhiteSpace(typed))
        {
            return null;
        }

        string trimmed = typed.Trim();

        if (trimmed.Any(character => !char.IsAsciiDigit(character) && character is not ('.' or ' ' or '-')))
        {
            throw new DomainException(OperationsItemErrors.CodeInvalid);
        }

        string digits = new(trimmed.Where(char.IsAsciiDigit).ToArray());
        return digits.Length is >= MinDigits and <= MaxDigits ? digits : throw new DomainException(OperationsItemErrors.CodeInvalid);
    }

    public static string Format(string? digits)
    {
        if (string.IsNullOrEmpty(digits) || digits.Length < MinDigits)
        {
            return digits ?? string.Empty;
        }

        string shown = string.Create(CultureInfo.InvariantCulture, $"{digits[..4]}.{digits[4..6]}");
        return digits.Length > MinDigits ? shown + "." + digits[6..] : shown;
    }
}
