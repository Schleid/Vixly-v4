using System.Globalization;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Settings;

// =========================================================
// FILE: OperationsCurrencies.cs
//
// PURPOSE:
// The ISO 4217 currencies a tenant may bill in (ADR-0018).
//
// RESPONSIBILITIES:
// - Know the codes; normalise and check one
//
// IMPORTANT NOTES:
// - Built from the regions .NET knows, like Platform
// =========================================================
public static class OperationsCurrencies
{
    private static readonly SortedSet<string> Known = BuildKnown();

    public static IReadOnlyCollection<string> All => Known;

    public static bool IsKnown(string? code) => code is not null && Known.Contains(code.Trim().ToUpperInvariant());

    public static string Normalize(string? code)
    {
        string value = code?.Trim().ToUpperInvariant() ?? string.Empty;

        return value.Length == OperationsSettingsLimits.CurrencyCodeLength && Known.Contains(value)
            ? value
            : throw new DomainException(OperationsSettingsErrors.CurrencyInvalid);
    }

    private static SortedSet<string> BuildKnown()
    {
        SortedSet<string> codes = new(StringComparer.Ordinal);

        foreach (CultureInfo culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
        {
            try
            {
                string symbol = new RegionInfo(culture.Name).ISOCurrencySymbol;

                if (symbol.Length == OperationsSettingsLimits.CurrencyCodeLength && symbol.All(char.IsAsciiLetterUpper))
                {
                    codes.Add(symbol);
                }
            }
            catch (ArgumentException)
            {
                // A culture without a region has no currency: skip it.
            }
        }

        // The currencies Vixly's tenants use today, whatever the host knows.
        codes.UnionWith(new[] { "USD", "HTG", "DOP", "EUR", "CAD", "XCD" });
        return codes;
    }
}
