using Vixly.Operations.Domain.Supplies;

namespace Vixly.Operations.Application.UseCases.Supplies.ListOperationsSupplyMovements;

// =========================================================
// FILE: ListOperationsSupplyMovementsRequest.cs
//
// PURPOSE:
// The stock movements of a supply at a warehouse, newest first (Slice 5E, S-4).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListOperationsSupplyMovementsRequest
{
    public Guid SupplyId { get; init; }

    public Guid WarehouseId { get; init; }
}
