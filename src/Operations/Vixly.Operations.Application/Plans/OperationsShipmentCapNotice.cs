namespace Vixly.Operations.Application.Plans;

// =========================================================
// FILE: OperationsShipmentCapNotice.cs
//
// PURPOSE:
// The outbox message of a shipment-cap warning to the Owner (shipment cap, owner 2026-10-29).
//
// RESPONSIBILITIES:
// - Carry the month, the level reached (80, 100, 125 or 150 percent), the numbers and whether this is the grace month
//
// IMPORTANT NOTES:
// - Written once per level and month by OperationsShipmentCapReport; sent by OperationsShipmentCapHandler
// =========================================================
public sealed record OperationsShipmentCapNotice(string Month, int Level, int Used, int Cap, int BlockAt, decimal ExtraShipmentPrice, string? CurrencyCode, bool GraceMonth)
{
    public const string MessageType = "ops.plans.shipment_cap.v1";
}
