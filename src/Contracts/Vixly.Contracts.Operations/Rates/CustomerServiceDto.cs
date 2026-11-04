namespace Vixly.Contracts.Operations.Rates;

// =========================================================
// FILE: CustomerServiceDto.cs
//
// PURPOSE:
// A service the company offers, with its own label (Slice 4).
//
// RESPONSIBILITIES:
// - Carry the code and label
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// =========================================================
public sealed record CustomerServiceDto(string Code, string? Label);
