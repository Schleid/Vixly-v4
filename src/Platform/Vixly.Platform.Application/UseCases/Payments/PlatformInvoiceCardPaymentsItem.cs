using Vixly.Platform.Domain.Payments;

namespace Vixly.Platform.Application.UseCases.Payments;

// =========================================================
// FILE: PlatformInvoiceCardPaymentsItem.cs
//
// PURPOSE:
// The card side of an invoice: transactions, the card that would be charged, what can be refunded.
//
// RESPONSIBILITIES:
// - Carry what the console or public page shows
//
// IMPORTANT NOTES:
// - Plain record; no card data beyond brand, last four and expiry
// =========================================================
public sealed record PlatformInvoiceCardPaymentsItem(
    IReadOnlyList<Vixly.Platform.Application.Queries.PlatformCardTransactionItem> Transactions,
    PlatformSavedCardItem? DefaultCard,
    bool CardPaymentsReady,
    bool CanCharge,
    bool CanRefund,
    IReadOnlyList<PlatformRefundableCreditItem> RefundableCredits);
