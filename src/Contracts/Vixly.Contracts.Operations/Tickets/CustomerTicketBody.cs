namespace Vixly.Contracts.Operations.Tickets;

// =========================================================
// FILE: CustomerTicketBody.cs
//
// PURPOSE:
// A customer opens a ticket about a package the carrier delivered that is not in its account (Slice 4D step 4).
//
// RESPONSIBILITIES:
// - Carry what the customer knows and the carrier's proof of delivery
//
// IMPORTANT NOTES:
// - Operations checks the warehouse, the sizes and the proof's type by its bytes
// =========================================================
public sealed record CustomerTicketBody(Guid? WarehouseId, string? Carrier, string? TrackingNumber, string? Store, string? OrderNumber, DateOnly? DeliveredOn, string? Contents, string? Message,
    byte[]? Proof);
