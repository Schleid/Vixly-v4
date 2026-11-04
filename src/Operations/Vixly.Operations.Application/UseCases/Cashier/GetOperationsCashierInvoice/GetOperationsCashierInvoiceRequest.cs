using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;

namespace Vixly.Operations.Application.UseCases.Cashier.GetOperationsCashierInvoice;

// =========================================================
// FILE: GetOperationsCashierInvoiceRequest.cs
//
// PURPOSE:
// One invoice at the cashier (Slice 5F, C-2 to C-5): what is owed, the methods the company accepts, the Zelle details and terms, the person's drawer, the payments so far.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsCashierInvoiceRequest
{
    public Guid InvoiceId { get; init; }
}
