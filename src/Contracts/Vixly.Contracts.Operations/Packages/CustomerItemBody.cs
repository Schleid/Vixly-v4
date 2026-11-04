namespace Vixly.Contracts.Operations.Packages;

// =========================================================
// FILE: CustomerItemBody.cs
//
// PURPOSE:
// One item the customer declares in a package (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - Carry the customer's words, the quantity, the value of one and whether it is used
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// =========================================================
public sealed record CustomerItemBody(string? Words, decimal Quantity, decimal ValueEach, bool Used);
