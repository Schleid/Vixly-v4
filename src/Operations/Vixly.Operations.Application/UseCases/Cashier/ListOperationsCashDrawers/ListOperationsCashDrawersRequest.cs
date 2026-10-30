using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;

namespace Vixly.Operations.Application.UseCases.Cashier.ListOperationsCashDrawers;

// =========================================================
// FILE: ListOperationsCashDrawersRequest.cs
//
// PURPOSE:
// Cash drawers (Slice 5F, C-4): the person's own, and every drawer of the warehouses where they confirm payments.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListOperationsCashDrawersRequest
{

}
