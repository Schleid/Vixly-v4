namespace Vixly.Contracts.Operations.Rates;

// =========================================================
// FILE: CustomerPricingDto.cs
//
// PURPOSE:
// The company's prices for the customer portal's Pricing page (owner, 2026-10-21): the regular prices and the customer's own.
//
// RESPONSIBILITIES:
// - Carry the values
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// =========================================================
public sealed record CustomerPricingDto(IReadOnlyList<CustomerPriceListDto> Lists, string WeightUnit, string DimensionUnit, string CurrencyCode);
