namespace Vixly.Operations.Application.UseCases.Inspection.ListOperationsInspectionQueue;

// =========================================================
// FILE: ListOperationsInspectionQueueRequest.cs
//
// PURPOSE:
// The inspection queue of the warehouses a person covers, oldest first (Slice 3C design section 5).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListOperationsInspectionQueueRequest
{
    public Guid? WarehouseId { get; init; }

    public int Page { get; init; } = 1;

    // A scanned parcel: its inspection, on whatever page it would be.
    public Guid? ParcelId { get; init; }
}
