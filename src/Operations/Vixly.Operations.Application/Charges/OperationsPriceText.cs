using System.Globalization;
using Vixly.Operations.Domain.Charges;

namespace Vixly.Operations.Application.Charges;

// =========================================================
// FILE: OperationsPriceText.cs
//
// PURPOSE:
// Reads the price lists staff type: one "value = price" pair per line (Slice 4).
//
// RESPONSIBILITIES:
// - Read insurance bands ("500 = 10") and plain numbers
// - Write them back for the form
//
// IMPORTANT NOTES:
// - A dot for decimals, whatever the language; null when a line cannot be read
// =========================================================
public static class OperationsPriceText
{
    private const int MaxLength = 4000;

    public static IReadOnlyList<OperationsInsuranceBand>? InsuranceBands(string? text) =>
        Pairs(text, number: true) is { } pairs ? pairs.Select(pair => new OperationsInsuranceBand(Number(pair.Key)!.Value, pair.Price)).ToList() : null;

    public static string Write(IEnumerable<(string Key, decimal Price)> pairs) =>
        string.Join("\n", pairs.Select(pair => pair.Key + " = " + pair.Price.ToString("0.####", CultureInfo.InvariantCulture)));

    public static string Write(decimal value) => value.ToString("0.####", CultureInfo.InvariantCulture);

    // A plain number: digits and one dot, no group separator, no sign.
    public static decimal? Number(string? value)
    {
        string text = value?.Trim() ?? string.Empty;
        return text.Length is > 0 and <= 20 && text.All(character => char.IsAsciiDigit(character) || character == '.')
            && decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal number)
            ? number
            : null;
    }

    private static List<(string Key, decimal Price)>? Pairs(string? text, bool number)
    {
        if (text is { Length: > MaxLength })
        {
            return null;
        }

        List<(string Key, decimal Price)> pairs = new();

        foreach (string line in (text ?? string.Empty).Split(['\n', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            int equals = line.LastIndexOf('=');

            if (equals <= 0 || Number(line[(equals + 1)..]) is not { } price)
            {
                return null;
            }

            string key = line[..equals].Trim();

            if (key.Length == 0 || (number && Number(key) is null))
            {
                return null;
            }

            pairs.Add((key, price));
        }

        return pairs;
    }
}
