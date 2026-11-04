namespace Vixly.Operations.Domain.Dispatch;

// =========================================================
// FILE: OperationsDispatchDraft.cs
//
// PURPOSE:
// What the dispatcher chooses for a new dispatch (Slice 6B, D-2).
//
// RESPONSIBILITIES:
// - Carry the origin, destination, service, cutoff and notes
//
// IMPORTANT NOTES:
// - Checked by OperationsDispatch.Create
// =========================================================
public sealed record OperationsDispatchDraft(Guid WarehouseId, Guid? DestinationWarehouseId, string? CountryCode, string? ServiceCode, DateTimeOffset? CutoffUtc, string? Notes)
{
    // Slice 7 (R-11): from a hub in the destination country to the final warehouse.
    public bool Onward { get; init; }

    // Slice 7 (review): who receives it; none for a country only.
    public Guid? ReceivingWarehouseId { get; init; }
}
