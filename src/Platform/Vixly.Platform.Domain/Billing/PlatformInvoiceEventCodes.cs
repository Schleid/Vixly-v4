namespace Vixly.Platform.Domain.Billing;

// =========================================================
// FILE: PlatformInvoiceEventCodes.cs
//
// PURPOSE:
// History codes for invoices; each has ui.invoice_history.* text in en, fr and es.
//
// RESPONSIBILITIES:
// - Name each history event of an invoice
//
// IMPORTANT NOTES:
// - Stored in InvoiceEvents; never rename one that shipped
// =========================================================
public static class PlatformInvoiceEventCodes
{
    public const string Issued = "platform.invoice_history.issued";

    public const string Emailed = "platform.invoice_history.emailed";

    public const string PaymentRecorded = "platform.invoice_history.payment_recorded";

    public const string PaymentReversed = "platform.invoice_history.payment_reversed";

    public const string CreditRequested = "platform.invoice_history.credit_requested";

    public const string CreditIssued = "platform.invoice_history.credit_issued";

    public const string CreditRejected = "platform.invoice_history.credit_rejected";

    public const string VoidRequested = "platform.invoice_history.void_requested";

    public const string Voided = "platform.invoice_history.voided";

    public const string PastDue = "platform.invoice_history.past_due";

    public const string CardRefunded = "platform.invoice_history.card_refunded";

    public const string CardFailed = "platform.invoice_history.card_failed";

    public const string CardActionRequired = "platform.invoice_history.card_action_required";

    public const string Disputed = "platform.invoice_history.disputed";

    public const string DisputeWon = "platform.invoice_history.dispute_won";

    public const string DisputeLost = "platform.invoice_history.dispute_lost";

    public static IReadOnlyList<string> All { get; } = new[]
    {
        Issued, Emailed, PaymentRecorded, PaymentReversed, CreditRequested, CreditIssued, CreditRejected, VoidRequested, Voided, PastDue,
        CardRefunded, CardFailed, CardActionRequired, Disputed, DisputeWon, DisputeLost
    };
}
