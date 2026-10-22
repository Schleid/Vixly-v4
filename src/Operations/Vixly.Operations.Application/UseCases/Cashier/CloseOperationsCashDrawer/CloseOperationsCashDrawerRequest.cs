using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;

namespace Vixly.Operations.Application.UseCases.Cashier.CloseOperationsCashDrawer;

// =========================================================
// FILE: CloseOperationsCashDrawerRequest.cs
//
// PURPOSE:
// Close the person's cash drawer with the count; the difference is recorded with a note (Slice 5F, C-4).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class CloseOperationsCashDrawerRequest
{
    public Guid DrawerId { get; init; }

    public decimal Counted { get; init; }

    public string? Note { get; init; }
}
