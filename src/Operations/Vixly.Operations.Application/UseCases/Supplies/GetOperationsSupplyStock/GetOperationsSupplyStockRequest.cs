using Vixly.Operations.Domain.Supplies;

namespace Vixly.Operations.Application.UseCases.Supplies.GetOperationsSupplyStock;

// =========================================================
// FILE: GetOperationsSupplyStockRequest.cs
//
// PURPOSE:
// The supplies stock of a warehouse (Slice 5E, S-4 to S-8): on hand, reorder levels, low stock, transfers in transit and the open count.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsSupplyStockRequest
{
    // None: the first warehouse the person keeps the stock of.
    public Guid? WarehouseId { get; init; }
}
