namespace Vixly.Contracts.Operations.Rates;

// =========================================================
// FILE: CustomerChargeLineDto.cs
//
// PURPOSE:
// One line of an estimate (Slice 4).
//
// RESPONSIBILITIES:
// - Carry the kind, name, piece, quantity and amount
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// - Kind: the engine's line kind, named on the page
// =========================================================
public sealed record CustomerChargeLineDto(string Kind, string? Label, int? Piece, decimal Quantity, decimal Amount);
