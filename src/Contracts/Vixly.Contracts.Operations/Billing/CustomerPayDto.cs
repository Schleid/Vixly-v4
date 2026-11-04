namespace Vixly.Contracts.Operations.Billing;

// =========================================================
// FILE: CustomerPayDto.cs
//
// PURPOSE:
// How to pay an invoice on the portal by Zelle or bank transfer (owner, 2026-10-27).
//
// RESPONSIBILITIES:
// - Carry the amount, the memo and the chosen method's details
//
// IMPORTANT NOTES:
// - Text: Zelle's terms or the bank transfer instructions, in the customer's language. Wire type: add fields at the end, never rename (ADR-0014)
// =========================================================
public sealed record CustomerPayDto(string InvoiceNumber, string Memo, decimal Amount, string CurrencyCode, string? Text, string? Tag, string? Contact, string? Name, bool HasQr);
