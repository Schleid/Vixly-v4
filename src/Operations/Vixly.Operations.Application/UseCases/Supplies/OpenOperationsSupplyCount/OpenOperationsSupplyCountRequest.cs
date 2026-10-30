using Vixly.Operations.Domain.Supplies;

namespace Vixly.Operations.Application.UseCases.Supplies.OpenOperationsSupplyCount;

// =========================================================
// FILE: OpenOperationsSupplyCountRequest.cs
//
// PURPOSE:
// A stock count opened at a warehouse (Slice 5E, S-7): every active supply, or one category.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class OpenOperationsSupplyCountRequest
{
    public Guid WarehouseId { get; init; }

    // None: every active supply.
    public OperationsSupplyCategory? Category { get; init; }
}
