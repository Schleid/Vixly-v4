using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;

namespace Vixly.Operations.Application.UseCases.Cashier.OpenOperationsCashDrawer;

// =========================================================
// FILE: OpenOperationsCashDrawerRequest.cs
//
// PURPOSE:
// Open the person's cash drawer at a warehouse with a starting float (Slice 5F, C-4).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class OpenOperationsCashDrawerRequest
{
    public Guid WarehouseId { get; init; }

    public decimal Float { get; init; }
}
