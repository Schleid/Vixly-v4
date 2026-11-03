namespace Vixly.Operations.Domain.Texts;

// =========================================================
// FILE: OperationsTextNotices.cs
//
// PURPOSE:
// The notices a company sends by SMS or WhatsApp when the customer chose them (Slice 9; owner, 2026-10-22: the company picks the events).
//
// RESPONSIBILITIES:
// - Name each notice as a flag
//
// IMPORTANT NOTES:
// - Stored as a number; never renumber
// - The rest always goes by email
// =========================================================
[Flags]
public enum OperationsTextNotices
{
    None = 0,
    ParcelReceived = 1,
    ActionNeeded = 2,
    ReadyForPickup = 4,
    PickupCode = 8,
    PickupReminder = 16,
    OutForDelivery = 32,
    DeliveryCode = 64,
    ShipmentUpdate = 128,
    PaymentNotReceived = 256
}
