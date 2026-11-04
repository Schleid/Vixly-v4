namespace Vixly.Contracts.Operations.Rates;

// =========================================================
// FILE: CustomerQuoteDto.cs
//
// PURPOSE:
// A quote the company made for the customer (Slice 4).
//
// RESPONSIBILITIES:
// - Carry the number, service, destination, totals and validity
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// - Never the staff member or the reason for a preferred price
// =========================================================
public sealed record CustomerQuoteDto(string Number, string ServiceCode, string? Country, decimal Total, decimal StandardTotal, DateTimeOffset ValidUntilUtc, IReadOnlyList<CustomerChargeLineDto> Lines, string Currency = "");
