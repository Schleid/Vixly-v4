namespace Vixly.Contracts.Operations.Billing;

// =========================================================
// FILE: CustomerCreditItemDto.cs
//
// PURPOSE:
// An open item of the customer's credit account (Slice 5F step 2b, K-8).
//
// RESPONSIBILITIES:
// - Carry the invoice, its dates and what is owed
//
// IMPORTANT NOTES:
// - Due none: waiting for the credit department
// =========================================================
public sealed record CustomerCreditItemDto(Guid InvoiceId, string Number, string Reference, DateOnly IssuedOn, DateOnly? DueOn, decimal Owed, bool Overdue);
