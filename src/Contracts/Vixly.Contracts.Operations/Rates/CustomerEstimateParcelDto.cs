namespace Vixly.Contracts.Operations.Rates;

// =========================================================
// FILE: CustomerEstimateParcelDto.cs
//
// PURPOSE:
// One parcel of a shipment estimate, in the company's units (Slice 5D).
//
// RESPONSIBILITIES:
// - Carry the values between Operations and the customer portal
//
// IMPORTANT NOTES:
// - Codes, numbers and the customer's own names only: the portal writes the words (ADR-0014)
// =========================================================
public sealed record CustomerEstimateParcelDto(int Number, string? Label, decimal Weight, decimal Length, decimal Width, decimal Height, decimal? Billable, decimal Amount);
