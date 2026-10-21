namespace Vixly.Contracts.Operations.Tickets;

// =========================================================
// FILE: CustomerTicketMessageDto.cs
//
// PURPOSE:
// One message on a ticket, as the customer sees it (Slice 4D step 4).
//
// RESPONSIBILITIES:
// - Carry who wrote (staff or the customer), the words and when
//
// IMPORTANT NOTES:
// - Staff are never named to the customer
// =========================================================
public sealed record CustomerTicketMessageDto(bool FromStaff, string Text, DateTimeOffset SentUtc);
