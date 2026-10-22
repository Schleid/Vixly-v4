using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Application.Delivery;

// =========================================================
// FILE: OperationsOutForDeliveryNotice.cs
//
// PURPOSE:
// Tell a customer their shipment is out for delivery today (Slice 7 step 3, R-7, R-15).
//
// RESPONSIBILITIES:
// - Carry the customer, the portal, the shipment and whether a delivery code is asked
//
// IMPORTANT NOTES:
// - The code itself is never written here: the handler makes it and sends it straight away
// =========================================================
public sealed record OperationsOutForDeliveryNotice(Guid CustomerId, string PortalSlug, string ShipmentNumber, bool WithCode)
{
    public const string MessageType = "ops.delivery.out_for_delivery.v1";
}
