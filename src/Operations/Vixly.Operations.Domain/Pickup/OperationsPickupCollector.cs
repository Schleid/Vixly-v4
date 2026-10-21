namespace Vixly.Operations.Domain.Pickup;

// =========================================================
// FILE: OperationsPickupCollector.cs
//
// PURPOSE:
// Who collected a shipment at the pickup counter (Slice 7 step 2, R-6).
//
// RESPONSIBILITIES:
// - The recipient, the person the customer named, or the customer
//
// IMPORTANT NOTES:
// - Stored as a number; never renumber
// =========================================================
public enum OperationsPickupCollector
{
    Recipient = 1,
    Named = 2,
    Customer = 3
}
