using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.Events;

// =========================================================
// FILE: OperationsEventSubject.cs
//
// PURPOSE:
// What an event is about, as the events workspace shows it (Slice 6C, E-6).
//
// RESPONSIBILITIES:
// - Carry the kind, id, number, the warehouses it leaves from and goes to, and its milestone for a parcel
//
// IMPORTANT NOTES:
// - A shipment, bundle or dispatch lists its parcels
// =========================================================
public sealed record OperationsEventSubject(OperationsTrackingSubject Kind, Guid Id, string Number, Guid? OriginWarehouseId, Guid? DestinationWarehouseId, OperationsMilestone? Milestone,
    string? Status)
{
    // Slice 7: where it is now (a hub on the way, or the warehouse it arrived at).
    public Guid? HereWarehouseId { get; init; }

    public bool Covers(IEnumerable<Guid> warehouseIds) =>
        warehouseIds.Any(id => id == OriginWarehouseId || id == DestinationWarehouseId || id == HereWarehouseId);
}
