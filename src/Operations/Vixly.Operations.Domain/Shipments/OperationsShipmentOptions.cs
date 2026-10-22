namespace Vixly.Operations.Domain.Shipments;

// =========================================================
// FILE: OperationsShipmentOptions.cs
//
// PURPOSE:
// What the customer asks for a shipment besides where it goes (owner, 2026-10-18).
//
// RESPONSIBILITIES:
// - Consolidate the parcels, keep them cold, insure them
//
// IMPORTANT NOTES:
// - Stored as flags; never renumber. Charges for each come with Slice 4
// =========================================================
[Flags]
public enum OperationsShipmentOptions
{
    None = 0,
    Consolidate = 1,
    Refrigerated = 2,
    Insured = 4,

    // Freight collect (owner, 2026-10-21): the sender pays at the destination. It moves unpaid; it is handed over once paid.
    FreightCollect = 8
}
