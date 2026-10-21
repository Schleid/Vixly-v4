namespace Vixly.Contracts.Operations.Tickets;

// =========================================================
// FILE: CustomerTicketReplyBody.cs
//
// PURPOSE:
// The customer writes on its ticket (Slice 4D step 4).
//
// RESPONSIBILITIES:
// - Carry the message
//
// IMPORTANT NOTES:
// - Up to 1,000 characters; the form's key makes a second send change nothing (ADR-0019)
// =========================================================
public sealed record CustomerTicketReplyBody(string? Message, string? Key = null);
