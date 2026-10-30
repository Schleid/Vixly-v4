namespace Vixly.Contracts.Operations.Rates;

// =========================================================
// FILE: CustomerPriceListDto.cs
//
// PURPOSE:
// A price list in force on the customer portal's Pricing page (owner, 2026-10-21).
//
// RESPONSIBILITIES:
// - Carry the values
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// =========================================================
public sealed record CustomerPriceListDto(string ServiceCode, string? ServiceLabel, string? Origin, string? Destination, bool Own, string Basis, string Mode, DateOnly EffectiveFrom,
    IReadOnlyList<CustomerRateBandDto> Bands, IReadOnlyList<CustomerPriceItemDto> Items, decimal? MinimumCharge, decimal? ServiceFee, decimal? ConsolidationFee, decimal? PerPackage,
    decimal? PerShipment);
