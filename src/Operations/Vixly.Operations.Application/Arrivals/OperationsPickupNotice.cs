using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Application.Arrivals;

// =========================================================
// FILE: OperationsPickupNotice.cs
//
// PURPOSE:
// Tell a customer their shipment is ready for pickup (Slice 7, R-1).
//
// RESPONSIBILITIES:
// - Carry the customer, the portal, the shipment and where to collect it
//
// IMPORTANT NOTES:
// - Written once, when the last parcel of the shipment becomes ready
// =========================================================
public sealed record OperationsPickupNotice(Guid CustomerId, string PortalSlug, string ShipmentNumber, string Place, string? Address, string? OpeningHours)
{
    public const string MessageType = "ops.arrivals.ready_for_pickup.v1";

    public static OperationsCustomerMailKind Kind => OperationsCustomerMailKind.ReadyForPickup;
}
