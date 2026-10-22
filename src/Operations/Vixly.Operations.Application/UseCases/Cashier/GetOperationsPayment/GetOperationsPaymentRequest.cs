using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;

namespace Vixly.Operations.Application.UseCases.Cashier.GetOperationsPayment;

// =========================================================
// FILE: GetOperationsPaymentRequest.cs
//
// PURPOSE:
// A payment just taken (Slice 5F, C-3): its tenders, the change to give and its invoice.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsPaymentRequest
{
    public Guid PaymentId { get; init; }
}
