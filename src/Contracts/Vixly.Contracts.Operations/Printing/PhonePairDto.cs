namespace Vixly.Contracts.Operations.Printing;

// =========================================================
// FILE: PhonePairDto.cs
//
// PURPOSE:
// The answer to a phone joining a warehouse (Slice 12E, owner H-6).
//
// RESPONSIBILITIES:
// - Carry the print program key, shown once, and the warehouse's name
//
// IMPORTANT NOTES:
// - Wire type between Operations and Vixly Print: add fields at the end, never rename (ADR-0014)
// - The phone keeps the key in its secure storage
// =========================================================
public sealed record PhonePairDto(Guid AgentId, string Key, string Warehouse);
