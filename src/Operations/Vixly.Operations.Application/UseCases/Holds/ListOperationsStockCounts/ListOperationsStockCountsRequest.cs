namespace Vixly.Operations.Application.UseCases.Holds.ListOperationsStockCounts;

// =========================================================
// FILE: ListOperationsStockCountsRequest.cs
//
// PURPOSE:
// The counts of the undeclared parcels area of the warehouses the staff member may see, newest first (Slice 4D step 3).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListOperationsStockCountsRequest
{
    public Guid? WarehouseId { get; init; }
}
