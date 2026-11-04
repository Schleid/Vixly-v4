using System.Text;

namespace Vixly.Operations.Domain.Shipments;

// =========================================================
// FILE: OperationsTrackingNumbers.cs
//
// PURPOSE:
// The shipment's tracking number (owner, 2026-10-28): two letters, then 11 digits, shown as "VX 482 917 36 054".
//
// RESPONSIBILITIES:
// - Make a number from 10 random digits and a check digit (Luhn), so a mistyped number is refused, never someone else's
// - Read a number typed with spaces, dashes or small letters
//
// IMPORTANT NOTES:
// - Random, never in sequence: nobody can guess another customer's number
// - The two letters are the company's (Settings, default VX)
// =========================================================
public static class OperationsTrackingNumbers
{
    public const string DefaultPrefix = "VX";

    public const int RandomDigits = 10;

    // Two letters and 11 digits, stored without spaces.
    public const int Length = 13;

    // The prefix is two capital letters A to Z.
    public static bool IsPrefix(string? prefix) =>
        prefix is { Length: 2 } && prefix.All(letter => letter is >= 'A' and <= 'Z');

    // The digits come from the caller (a secure random source in the application, fixed ones in tests).
    public static string Make(string prefix, IReadOnlyList<int> digits)
    {
        ArgumentNullException.ThrowIfNull(digits);

        if (!IsPrefix(prefix) || digits.Count != RandomDigits || digits.Any(digit => digit is < 0 or > 9))
        {
            throw new ArgumentException("Two capital letters and ten digits are needed.");
        }

        string body = string.Concat(digits);
        return prefix + body + CheckDigit(body);
    }

    // The stored form of what someone typed, or null when it is not a valid tracking number.
    public static string? Read(string? text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 40)
        {
            return null;
        }

        StringBuilder kept = new(Length);

        foreach (char character in text)
        {
            if (character is ' ' or '-' or '.' or '\t')
            {
                continue;
            }

            kept.Append(char.ToUpperInvariant(character));
        }

        string number = kept.ToString();

        return number.Length == Length && IsPrefix(number[..2]) && number[2..].All(char.IsAsciiDigit)
            && CheckDigit(number[2..^1]) == number[^1] ? number : null;
    }

    // "VX48291736054" -> "VX 482 917 36 054".
    public static string Format(string? number) =>
        number is { Length: Length } ? $"{number[..2]} {number[2..5]} {number[5..8]} {number[8..10]} {number[10..]}" : number ?? string.Empty;

    // Luhn over the ten digits: catches every single wrong digit and most swapped pairs.
    private static char CheckDigit(string body)
    {
        int sum = 0;

        for (int index = 0; index < body.Length; index++)
        {
            int digit = body[body.Length - 1 - index] - '0';

            if (index % 2 == 0)
            {
                digit *= 2;
                digit = digit > 9 ? digit - 9 : digit;
            }

            sum += digit;
        }

        return (char)('0' + ((10 - (sum % 10)) % 10));
    }
}
