using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.Shipments;

// =========================================================
// FILE: ShipmentParcels.cs
//
// PURPOSE:
// Which parcels of a shipment still travel with it, and whether one has left the origin (Slice 5A, review).
//
// RESPONSIBILITIES:
// - Leave out parcels with an end milestone (cancelled, returned to sender, disposed of or sold)
// - Tell a parcel on its way from one at the origin
//
// IMPORTANT NOTES:
// - The end milestones sit above the journey's (ADR-0018): they are not "departed"
// =========================================================
public static class ShipmentParcels
{
    public static bool Ended(OperationsParcel parcel) => parcel?.Milestone is >= OperationsMilestone.Cancelled;

    public static bool Departed(OperationsParcel parcel) => parcel is not null && !Ended(parcel) && !parcel.IsAtOrigin;

    public static IReadOnlyList<OperationsParcel> Active(IReadOnlyList<OperationsParcel> parcels) => parcels.Where(parcel => !Ended(parcel)).ToList();
}
