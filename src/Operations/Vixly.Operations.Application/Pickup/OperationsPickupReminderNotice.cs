namespace Vixly.Operations.Application.Pickup;

// =========================================================
// FILE: OperationsPickupReminderNotice.cs
//
// PURPOSE:
// Remind a customer that their shipment waits at its pickup counter (Slice 7 step 2, R-10).
//
// RESPONSIBILITIES:
// - Carry the customer, the portal, the shipment, the warehouse and whether it is the final notice
//
// IMPORTANT NOTES:
// - Written by the reminder job; the handler words it
// =========================================================
public sealed record OperationsPickupReminderNotice(Guid CustomerId, string PortalSlug, string ShipmentNumber, Guid WarehouseId, bool Final)
{
    public const string MessageType = "ops.pickup.reminder.v1";
}
