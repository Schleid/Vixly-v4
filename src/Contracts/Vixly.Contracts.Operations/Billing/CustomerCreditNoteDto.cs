namespace Vixly.Contracts.Operations.Billing;

// =========================================================
// FILE: CustomerCreditNoteDto.cs
//
// PURPOSE:
// An approved credit note of the customer's invoice (Slice 5F, I-6).
//
// RESPONSIBILITIES:
// - Carry its number, date and amount
//
// IMPORTANT NOTES:
// - Requests waiting for a decision are never sent
// =========================================================
public sealed record CustomerCreditNoteDto(Guid CreditNoteId, string Number, DateOnly IssuedOn, decimal Total);
