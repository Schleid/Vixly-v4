using Vixly.Operations.Domain.Charges;

namespace Vixly.Operations.Application.Charges;

// =========================================================
// FILE: OperationsQuoteRow.cs
//
// PURPOSE:
// A quote with its customer, for the quotes page (Slice 4).
//
// RESPONSIBILITIES:
// - Carry the quote and its customer's code and name
//
// IMPORTANT NOTES:
// - Staff only
// =========================================================
public sealed record OperationsQuoteRow(OperationsQuote Quote, string? CustomerCode, string CustomerName);
