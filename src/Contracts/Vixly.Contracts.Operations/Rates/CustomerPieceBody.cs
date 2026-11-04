namespace Vixly.Contracts.Operations.Rates;

// =========================================================
// FILE: CustomerPieceBody.cs
//
// PURPOSE:
// One piece the customer types in the calculator, in the company's units (Slice 4).
//
// RESPONSIBILITIES:
// - Carry the weight, size and value
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// =========================================================
public sealed record CustomerPieceBody(decimal? Weight, decimal? Length, decimal? Width, decimal? Height, decimal? Value);
