namespace Vixly.Operations.Application.Support;

// =========================================================
// FILE: OperationsSupportMasks.cs
//
// PURPOSE:
// Hides most of a phone number, an email and a name for support agents (owner, 2026-10-29, design 9.2).
//
// RESPONSIBILITIES:
// - Phone: the last four digits (•••-•••-4521)
// - Email: the first letter of each part and the ending (j•••@g•••.com)
// - Name: the first name and the last initial (Jean P.)
// - Compare two phone numbers by their last digits
//
// IMPORTANT NOTES:
// - Pure functions
// =========================================================
public static class OperationsSupportMasks
{
    private const string Dots = "\u2022\u2022\u2022";

    public static string Digits(string? value) => new((value ?? string.Empty).Where(char.IsAsciiDigit).ToArray());

    public static string? Phone(string? phone)
    {
        string digits = Digits(phone);
        return digits.Length < 4 ? null : Dots + "-" + Dots + "-" + digits[^4..];
    }

    public static string? Email(string? email)
    {
        string text = (email ?? string.Empty).Trim();
        int at = text.IndexOf('@', StringComparison.Ordinal);

        if (at < 1 || at == text.Length - 1)
        {
            return null;
        }

        string domain = text[(at + 1)..];
        int dot = domain.LastIndexOf('.');
        string ending = dot > 0 ? domain[dot..] : string.Empty;
        return text[0] + Dots + "@" + domain[0] + Dots + ending;
    }

    public static string ShortName(string? firstName, string? lastName)
    {
        string first = (firstName ?? string.Empty).Trim();
        string last = (lastName ?? string.Empty).Trim();
        return last.Length == 0 ? first : (first + " " + char.ToUpperInvariant(last[0]) + ".").Trim();
    }

    // The same number written two ways (+509 4860-5107 and 48605107): the same last 7 to 10 digits.
    public static bool SamePhone(string? first, string? second)
    {
        string a = Digits(first);
        string b = Digits(second);
        int length = Math.Min(Math.Min(a.Length, b.Length), 10);
        return length >= 7 && string.Equals(a[^length..], b[^length..], StringComparison.Ordinal);
    }
}
