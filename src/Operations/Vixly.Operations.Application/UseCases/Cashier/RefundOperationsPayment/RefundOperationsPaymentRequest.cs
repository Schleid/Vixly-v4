using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;

namespace Vixly.Operations.Application.UseCases.Cashier.RefundOperationsPayment;

// =========================================================
// FILE: RefundOperationsPaymentRequest.cs
//
// PURPOSE:
// Give money back after a credit note on a paid invoice (Slice 5F, F-7 and C-8).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class RefundOperationsPaymentRequest
{
    public Guid InvoiceId { get; init; }

    public OperationsPaymentMethod Method { get; init; }

    public decimal Amount { get; init; }

    public string? Reason { get; init; }

    public string? Reference { get; init; }

    public Guid RequestKey { get; init; }
}
