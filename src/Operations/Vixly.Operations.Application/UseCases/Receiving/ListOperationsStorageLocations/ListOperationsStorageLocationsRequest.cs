namespace Vixly.Operations.Application.UseCases.Receiving.ListOperationsStorageLocations;

// =========================================================
// FILE: ListOperationsStorageLocationsRequest.cs
//
// PURPOSE:
// The shelves of one warehouse (Slice 3B).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListOperationsStorageLocationsRequest
{
    public Guid WarehouseId { get; init; }
}
