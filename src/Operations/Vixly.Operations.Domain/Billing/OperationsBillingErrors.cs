using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Billing;

// =========================================================
// FILE: OperationsBillingErrors.cs
//
// PURPOSE:
// Error codes of invoices and credit notes (Slice 5F step 1).
//
// RESPONSIBILITIES:
// - Name every refusal about an invoice, a credit note and the invoice settings
//
// IMPORTANT NOTES:
// - Codes, never text (L-1)
// =========================================================
public static class OperationsBillingErrors
{
    public static Error InvoiceNotFound { get; } = Error.Create("ops.invoices.invoice_not_found");

    public static Error CreditNotFound { get; } = Error.Create("ops.invoices.credit_not_found");

    public static Error NothingToInvoice { get; } = Error.Create("ops.invoices.nothing_to_invoice");

    public static Error Voided { get; } = Error.Create("ops.invoices.voided");

    public static Error ReasonRequired { get; } = Error.Create("ops.invoices.reason_required");

    public static Error AmountInvalid { get; } = Error.Create("ops.invoices.amount_invalid");

    public static Error NothingToCredit { get; } = Error.Create("ops.invoices.nothing_to_credit");

    public static Error VoidNotPossible { get; } = Error.Create("ops.invoices.void_not_possible");

    public static Error RequestOpen { get; } = Error.Create("ops.invoices.request_open");

    public static Error NotRequested { get; } = Error.Create("ops.invoices.not_requested");

    public static Error OwnRequest { get; } = Error.Create("ops.invoices.own_request");

    public static Error VoidRequired { get; } = Error.Create("ops.invoices.void_required");

    public static Error TaxRateInvalid { get; } = Error.Create("ops.invoices.tax_rate_invalid");

    public static Error TextInvalid { get; } = Error.Create("ops.invoices.text_invalid");

    // Freight collect chosen at the cashier (owner, 2026-10-27): only on an unpaid shipment invoice without supplies.
    public static Error CollectNotPossible { get; } = Error.Create("ops.invoices.collect_not_possible");

    public static Error CollectAfterPayment { get; } = Error.Create("ops.invoices.collect_after_payment");

    public static Error CollectWithSupplies { get; } = Error.Create("ops.invoices.collect_with_supplies");

    public static Error CollectDeparted { get; } = Error.Create("ops.invoices.collect_departed");
}
