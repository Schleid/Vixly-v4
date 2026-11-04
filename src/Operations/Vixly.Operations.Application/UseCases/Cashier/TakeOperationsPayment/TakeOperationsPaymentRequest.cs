using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;

namespace Vixly.Operations.Application.UseCases.Cashier.TakeOperationsPayment;

// =========================================================
// FILE: TakeOperationsPaymentRequest.cs
//
// PURPOSE:
// Take the payment of an invoice in full, with one or more methods (Slice 5F, F-4, C-3 to C-5).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class TakeOperationsPaymentRequest
{
    public Guid InvoiceId { get; init; }

    public IReadOnlyList<OperationsTenderDraft> Tenders { get; init; } = Array.Empty<OperationsTenderDraft>();

    // The customer accepted the company's Zelle terms (P-9).
    public bool ZelleTermsAccepted { get; init; }

    // The form's key (ADR-0019): sent twice, one payment.
    public Guid RequestKey { get; init; }
}
