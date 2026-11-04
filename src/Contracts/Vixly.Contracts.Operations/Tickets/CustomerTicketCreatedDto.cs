namespace Vixly.Contracts.Operations.Tickets;

// =========================================================
// FILE: CustomerTicketCreatedDto.cs
//
// PURPOSE:
// The ticket the customer just opened (Slice 4D step 4).
//
// RESPONSIBILITIES:
// - Carry its id
//
// IMPORTANT NOTES:
// - None
// =========================================================
public sealed record CustomerTicketCreatedDto(Guid TicketId);
