namespace Vixly.Contracts.Operations.Rates;

// =========================================================
// FILE: CustomerRateBandDto.cs
//
// PURPOSE:
// One band of a service's rates, shown while no parcel is in (Slice 5D).
//
// RESPONSIBILITIES:
// - Carry the values between Operations and the customer portal
//
// IMPORTANT NOTES:
// - Codes, numbers and the customer's own names only: the portal writes the words (ADR-0014)
// =========================================================
public sealed record CustomerRateBandDto(string Kind, decimal From, decimal Amount, decimal? Base);
