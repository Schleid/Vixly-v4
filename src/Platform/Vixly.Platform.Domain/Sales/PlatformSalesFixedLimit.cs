namespace Vixly.Platform.Domain.Sales;

// =========================================================
// FILE: PlatformSalesFixedLimit.cs
//
// PURPOSE:
// The largest fixed discount a salesperson may grant alone, in one currency (D-S22).
//
// RESPONSIBILITIES:
// - Hold a currency and an amount
//
// IMPORTANT NOTES:
// - Owned by PlatformSalesSettings; one per currency
// =========================================================
public sealed class PlatformSalesFixedLimit
{
    private PlatformSalesFixedLimit()
    {
        CurrencyCode = string.Empty;
    }

    internal PlatformSalesFixedLimit(string currencyCode, decimal amount)
    {
        CurrencyCode = currencyCode;
        Amount = amount;
    }

    public string CurrencyCode { get; private set; }

    public decimal Amount { get; private set; }
}
