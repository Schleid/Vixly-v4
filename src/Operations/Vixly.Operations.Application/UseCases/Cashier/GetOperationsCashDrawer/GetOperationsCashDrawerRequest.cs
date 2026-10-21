using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;

namespace Vixly.Operations.Application.UseCases.Cashier.GetOperationsCashDrawer;

// =========================================================
// FILE: GetOperationsCashDrawerRequest.cs
//
// PURPOSE:
// A cash drawer's report (Slice 5F, C-4): float, cash in, cash out, expected, counted, difference, and each payment and refund in cash.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsCashDrawerRequest
{
    public Guid DrawerId { get; init; }
}
