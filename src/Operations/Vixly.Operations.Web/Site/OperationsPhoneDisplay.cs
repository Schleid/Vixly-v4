namespace Vixly.Operations.Web.Site;

// =========================================================
// FILE: OperationsPhoneDisplay.cs
//
// PURPOSE:
// Shows a stored international phone number the way people read it.
//
// RESPONSIBILITIES:
// - Group North American (+1) and Haitian (+509) numbers
// - Leave other numbers as stored
//
// IMPORTANT NOTES:
// - Display only: links (tel:) always use the stored number
// =========================================================
public static class OperationsPhoneDisplay
{
    public static string Format(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return string.Empty;
        }

        string raw = phone.Trim();
        bool hasPlus = raw.StartsWith('+');
        string digits = string.Concat(raw.Where(char.IsAsciiDigit));

        if (digits.Length == 10)
        {
            return hasPlus ? $"+1 ({digits[..3]}) {digits[3..6]}-{digits[6..]}" : $"({digits[..3]}) {digits[3..6]}-{digits[6..]}";
        }

        if (digits.Length == 11 && digits.StartsWith('1'))
        {
            return $"+1 ({digits[1..4]}) {digits[4..7]}-{digits[7..]}";
        }

        if (digits.Length == 8)
        {
            return $"{digits[..4]}-{digits[4..]}";
        }

        if (digits.Length == 11 && digits.StartsWith("509", StringComparison.Ordinal))
        {
            return $"+509 {digits[3..7]}-{digits[7..]}";
        }

        return raw;
    }
}
