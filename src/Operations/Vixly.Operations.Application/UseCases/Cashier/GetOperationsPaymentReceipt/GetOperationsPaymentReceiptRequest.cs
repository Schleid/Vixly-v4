using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;

namespace Vixly.Operations.Application.UseCases.Cashier.GetOperationsPaymentReceipt;

// =========================================================
// FILE: GetOperationsPaymentReceiptRequest.cs
//
// PURPOSE:
// The receipt of a payment as a PDF, in the customer's language (Slice 5F, C-3).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsPaymentReceiptRequest
{
    public Guid PaymentId { get; init; }
}
