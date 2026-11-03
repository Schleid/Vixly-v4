using Vixly.Operations.Domain.Billing;

namespace Vixly.Operations.Application.UseCases.Billing.PayMyOperationsInvoice;

// =========================================================
// FILE: PayMyOperationsInvoiceRequest.cs
//
// PURPOSE:
// A customer paying an invoice on the portal by Zelle or bank transfer (owner, 2026-10-27).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Report false: only read what to pay and how; true: the customer says the money is sent
// =========================================================
public sealed class PayMyOperationsInvoiceRequest
{
    public string SessionToken { get; init; } = string.Empty;

    public Guid InvoiceId { get; init; }

    public OperationsPaymentMethod Method { get; init; }

    public bool Report { get; init; }

    // Zelle only: the company's terms, accepted before paying (P-9).
    public bool TermsAccepted { get; init; }

    // The Zelle confirmation number or the transfer reference; finance matches the money by it.
    public string? Reference { get; init; }

    // The form's key: sent twice, one payment (ADR-0019).
    public Guid RequestKey { get; init; }
}
