namespace Vixly.Tools.V3Migration;

// =========================================================
// FILE: Phones.cs
//
// PURPOSE:
// Turns V3's free-form phone numbers into V4's international form (+ and digits).
//
// RESPONSIBILITIES:
// - Keep a number that already starts with +
// - Add the country code from the country written in V3 (United States, Haiti) when it is missing
//
// IMPORTANT NOTES:
// - A number it cannot read is reported, never guessed
// - Mask shows only the last two digits, for the report
// =========================================================
internal static class Phones
{
    public static string? Normalize(string? value, string? country)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string digits = new(value.Where(char.IsAsciiDigit).ToArray());

        if (value.TrimStart().StartsWith('+'))
        {
            return digits.Length is >= 8 and <= 15 ? "+" + digits : null;
        }

        string place = (country ?? string.Empty).Trim().ToUpperInvariant().Replace("Ï", "I", StringComparison.Ordinal);

        return digits.Length switch
        {
            11 when digits.StartsWith("509", StringComparison.Ordinal) => "+" + digits,
            11 when digits.StartsWith('1') => "+" + digits,
            10 when place is "US" or "USA" or "UNITED STATES" => "+1" + digits,
            8 when place is "HT" or "HAITI" => "+509" + digits,
            _ => null
        };
    }

    public static string Mask(string? value) =>
        string.IsNullOrEmpty(value) ? "(empty)" : new string('•', Math.Max(0, value.Length - 2)) + value[^Math.Min(2, value.Length)..];
}
