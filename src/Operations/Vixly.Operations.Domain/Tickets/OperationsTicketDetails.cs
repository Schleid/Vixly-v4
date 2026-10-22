namespace Vixly.Operations.Domain.Tickets;

// =========================================================
// FILE: OperationsTicketDetails.cs
//
// PURPOSE:
// What the customer knows about a missing package (Slice 4D step 4, A-6).
//
// RESPONSIBILITIES:
// - Carrier, tracking number, store and order number, the day the carrier delivered it, what is inside
//
// IMPORTANT NOTES:
// - Checked by OperationsTicket
// =========================================================
public sealed record OperationsTicketDetails(string? Carrier, string? TrackingNumber, string? Store, string? OrderNumber, DateOnly? DeliveredOn, string? Contents);
