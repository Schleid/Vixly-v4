namespace Vixly.Contracts.Operations.Rates;

// =========================================================
// FILE: CustomerPriceItemDto.cs
//
// PURPOSE:
// A price per item on a price list by size (owner, 2026-10-21).
//
// RESPONSIBILITIES:
// - Carry the values
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// =========================================================
public sealed record CustomerPriceItemDto(string Name, decimal Price);
