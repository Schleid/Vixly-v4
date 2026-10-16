using System.Globalization;

namespace Vixly.Platform.Domain.Payments;

// =========================================================
// FILE: PlatformPaymentsLimits.cs
//
// PURPOSE:
// Sizes, lifetimes and currency units for card payments.
//
// RESPONSIBILITIES:
// - Hold the limits the domain and database share
// - Convert an amount to the provider's minor units
//
// IMPORTANT NOTES:
// - Amounts are always computed by the server, never read from a browser (D-P7)
// =========================================================
public static class PlatformPaymentsLimits
{
    public const string SingletonKey = "default";

    public const int ProviderIdMaxLength = 255;

    public const int BrandMaxLength = 20;

    public const int MaxCurrencies = 10;

    public const int PublishableKeyMaxLength = 255;

    public const int FailureCodeMaxLength = 60;

    public const int EventTypeMaxLength = 100;

    public const int OutcomeMaxLength = 40;

    public const int ConsentVersionMaxLength = 20;

    public const int TokenHashLength = 64;

    public static TimeSpan PayLinkLifetime { get; } = TimeSpan.FromDays(30);

    public static TimeSpan SaveCardLinkLifetime { get; } = TimeSpan.FromDays(7);

    // Open transactions older than this are checked with the provider (D-P5).
    public static TimeSpan StaleAfter { get; } = TimeSpan.FromMinutes(30);

    // Currencies the provider counts without cents.
    private static readonly HashSet<string> ZeroDecimal = new(StringComparer.Ordinal)
    {
        "BIF", "CLP", "DJF", "GNF", "JPY", "KMF", "KRW", "MGA", "PYG", "RWF", "UGX", "VND", "VUV", "XAF", "XOF", "XPF"
    };

    public static long ToMinorUnits(decimal amount, string currencyCode)
    {
        ArgumentNullException.ThrowIfNull(currencyCode);

        decimal scaled = ZeroDecimal.Contains(currencyCode) ? amount : amount * 100m;

        if (amount <= 0m || decimal.Round(scaled, 0) != scaled)
        {
            throw new Vixly.Shared.Kernel.Domain.DomainException(PlatformPaymentsErrors.AmountInvalid);
        }

        return decimal.ToInt64(scaled);
    }

    public static decimal FromMinorUnits(long minor, string currencyCode)
    {
        ArgumentNullException.ThrowIfNull(currencyCode);

        return ZeroDecimal.Contains(currencyCode) ? minor : minor / 100m;
    }

    public static string Currency(string? currencyCode)
    {
        string code = currencyCode?.Trim().ToUpperInvariant() ?? string.Empty;

        if (code.Length != 3 || !code.All(character => character is >= 'A' and <= 'Z'))
        {
            throw new Vixly.Shared.Kernel.Domain.DomainException(PlatformPaymentsErrors.CurrencyInvalid);
        }

        return code;
    }

    public static string ProviderId(string? value)
    {
        string trimmed = value?.Trim() ?? string.Empty;

        if (trimmed.Length == 0 || trimmed.Length > ProviderIdMaxLength || trimmed.Any(char.IsWhiteSpace))
        {
            throw new Vixly.Shared.Kernel.Domain.DomainException(PlatformPaymentsErrors.ProviderReferenceInvalid);
        }

        return trimmed;
    }

    public static string Code(string? value, int maxLength)
    {
        string trimmed = value?.Trim() ?? string.Empty;

        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    public static string Format(decimal amount)
    {
        return amount.ToString("0.00", CultureInfo.InvariantCulture);
    }
}
