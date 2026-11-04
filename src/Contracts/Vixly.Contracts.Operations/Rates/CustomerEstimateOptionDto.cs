namespace Vixly.Contracts.Operations.Rates;

// =========================================================
// FILE: CustomerEstimateOptionDto.cs
//
// PURPOSE:
// Another choice in a shipment estimate and the total it would make (Slice 5D). Kind: none, product, consolidate, separate.
//
// RESPONSIBILITIES:
// - Carry the values between Operations and the customer portal
//
// IMPORTANT NOTES:
// - Codes, numbers and the customer's own names only: the portal writes the words (ADR-0014)
// =========================================================
public sealed record CustomerEstimateOptionDto(string Kind, Guid? PlanId, string? Name, decimal? Change, decimal? Total, bool Current);
