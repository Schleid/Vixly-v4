namespace Vixly.Platform.Application.SalesFeed;

// =========================================================
// FILE: PlatformSalesFeedTypes.cs
//
// PURPOSE:
// The event types of the sales feed to TechOS (contract version 1).
//
// RESPONSIBILITIES:
// - Name each type exactly as the contract spells it
//
// IMPORTANT NOTES:
// - Contract values; never rename
// - Totalled lists every type the daily totals count (all but totals.daily itself)
// =========================================================
public static class PlatformSalesFeedTypes
{
    public const string InvoiceIssued = "invoice.issued";

    public const string PaymentReceived = "payment.received";

    public const string PaymentReversed = "payment.reversed";

    public const string InvoiceVoided = "invoice.voided";

    public const string InvoiceCredited = "invoice.credited";

    public const string SubscriptionChanged = "subscription.changed";

    public const string TotalsDaily = "totals.daily";

    public static IReadOnlyList<string> Totalled { get; } = new[]
    {
        InvoiceIssued, PaymentReceived, PaymentReversed, InvoiceVoided, InvoiceCredited, SubscriptionChanged
    };
}
