using Vixly.Operations.Domain.Delivery;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.Delivery;

// =========================================================
// FILE: OperationsDeliveryRouteView.cs
//
// PURPOSE:
// A delivery route loaded for a change (Slice 7 step 3).
//
// RESPONSIBILITIES:
// - The route, its warehouse, its stops, their shipments and the parcels on the route (all tracked)
//
// IMPORTANT NOTES:
// - Built by the delivery desk only
// =========================================================
public sealed record OperationsDeliveryRouteView(OperationsDeliveryRoute Route, OperationsWarehouse Warehouse, IReadOnlyList<OperationsDeliveryStop> Stops,
    IReadOnlyDictionary<Guid, OperationsShipment> Shipments, IReadOnlyList<OperationsParcel> Parcels)
{
    public IReadOnlyList<OperationsParcel> ParcelsOf(Guid shipmentId) => Parcels.Where(parcel => parcel.ShipmentId == shipmentId).ToList();
}
