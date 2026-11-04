namespace Vixly.Contracts.Operations.Packages;

// =========================================================
// FILE: CustomerChoiceDto.cs
//
// PURPOSE:
// One choice on a customer form: a mailbox warehouse or a pickup branch (owner, 2026-10-18).
//
// RESPONSIBILITIES:
// - Carry the id, the name and where it is
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// =========================================================
public sealed record CustomerChoiceDto(Guid Id, string Name, string? Detail);
