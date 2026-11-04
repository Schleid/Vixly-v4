using Vixly.Operations.Domain.Supplies;

namespace Vixly.Operations.Application.UseCases.Supplies.AdjustOperationsSupplyStock;

// =========================================================
// FILE: AdjustOperationsSupplyStockRequest.cs
//
// PURPOSE:
// Supplies stock adjusted by hand at a warehouse (Slice 5E, S-4): more or fewer, with a reason and a note.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class AdjustOperationsSupplyStockRequest
{
    public Guid WarehouseId { get; init; }

    public Guid SupplyId { get; init; }

    // More: positive; fewer: negative.
    public int Quantity { get; init; }

    public OperationsSupplyAdjustReason Reason { get; init; }

    public string? Note { get; init; }

    // The form's key: the same form sent twice moves the stock once (ADR-0019).
    public Guid Key { get; init; }
}
