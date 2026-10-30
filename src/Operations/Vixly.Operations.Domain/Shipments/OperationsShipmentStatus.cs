namespace Vixly.Operations.Domain.Shipments;

// =========================================================
// FILE: OperationsShipmentStatus.cs
//
// PURPOSE:
// State of a shipment (Slice 3B; booking arrives with Slice 5).
//
// RESPONSIBILITIES:
// - Open while parcels gather; booked once service and charges are chosen
//
// IMPORTANT NOTES:
// - Stored; never renumber
// =========================================================
public enum OperationsShipmentStatus
{
    Open = 0,
    Booked = 1,
    Cancelled = 2
}
