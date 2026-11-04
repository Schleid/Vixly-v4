namespace Vixly.Contracts.Operations.Tickets;

// =========================================================
// FILE: CustomerTicketsDto.cs
//
// PURPOSE:
// The customer's tickets and the warehouses it can name on a new one (Slice 4D step 4).
//
// RESPONSIBILITIES:
// - Carry the list and the choices
//
// IMPORTANT NOTES:
// - Newest first, up to 50
// =========================================================
public sealed record CustomerTicketsDto(IReadOnlyList<CustomerTicketDto> Tickets, IReadOnlyList<Vixly.Contracts.Operations.Packages.CustomerChoiceDto> Warehouses);
