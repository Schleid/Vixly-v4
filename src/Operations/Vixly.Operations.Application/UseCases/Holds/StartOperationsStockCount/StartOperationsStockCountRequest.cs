namespace Vixly.Operations.Application.UseCases.Holds.StartOperationsStockCount;

// =========================================================
// FILE: StartOperationsStockCountRequest.cs
//
// PURPOSE:
// Starts a count of the undeclared parcels area of a warehouse: every package nobody could place, with its shelf (Slice 4D step 3).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class StartOperationsStockCountRequest
{
    public Guid WarehouseId { get; init; }
}
