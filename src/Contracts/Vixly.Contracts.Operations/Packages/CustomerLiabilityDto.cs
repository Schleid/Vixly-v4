namespace Vixly.Contracts.Operations.Packages;

// =========================================================
// FILE: CustomerLiabilityDto.cs
//
// PURPOSE:
// What the company owes, for one transport mode, when the customer does not insure the shipment (Slice 5B, I-3).
//
// RESPONSIBILITIES:
// - Carry the basis (None, PerWeight, PerParcel, DeclaredValueUpToCap), the amount and its unit, the cap and the company's words
//
// IMPORTANT NOTES:
// - Shown before the customer chooses not to insure
// =========================================================
public sealed record CustomerLiabilityDto(string Mode, string Basis, decimal? Amount, string Unit, decimal? Cap, string? Text, string Currency, bool CapPerShipment = false);
