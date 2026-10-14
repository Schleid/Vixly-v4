using System.Globalization;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Subscriptions;

// =========================================================
// FILE: PlatformCurrencies.cs
//
// PURPOSE:
// The ISO 4217 currency codes a plan may use.
//
// RESPONSIBILITIES:
// - Normalise and check a currency code against the codes .NET knows
//
// IMPORTANT NOTES:
// - One currency per plan (D-S7)
// - The list comes from the regions .NET knows, so it is the same on every server
// =========================================================
public static class PlatformCurrencies
{
    private static readonly HashSet<string> Known = BuildKnown();

    public static bool IsKnown(string? code)
    {
        return code is not null && Known.Contains(code.Trim().ToUpperInvariant());
    }

    public static string Normalize(string? code)
    {
        string value = code?.Trim().ToUpperInvariant() ?? string.Empty;

        if (value.Length != PlatformSubscriptionLimits.CurrencyCodeLength || !Known.Contains(value))
        {
            throw new DomainException(PlatformPlanErrors.CurrencyInvalid);
        }

        return value;
    }

    private static HashSet<string> BuildKnown()
    {
        HashSet<string> codes = new(StringComparer.Ordinal);

        foreach (CultureInfo culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
        {
            try
            {
                string symbol = new RegionInfo(culture.Name).ISOCurrencySymbol;

                if (symbol.Length == PlatformSubscriptionLimits.CurrencyCodeLength && symbol.All(char.IsAsciiLetterUpper))
                {
                    codes.Add(symbol);
                }
            }
            catch (ArgumentException)
            {
                // A culture without a region has no currency: skip it.
            }
        }

        codes.Add("USD");
        codes.Add("CAD");
        codes.Add("EUR");

        return codes;
    }
}
