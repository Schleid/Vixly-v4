namespace Vixly.Contracts.Operations.Packages;

// =========================================================
// FILE: CustomerHoldAnswerDto.cs
//
// PURPOSE:
// The customer's latest answer to one hold (Slice 3E).
//
// RESPONSIBILITIES:
// - Carry the hold, what was sent, where it stands, and why it was declined
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// =========================================================
public sealed record CustomerHoldAnswerDto(string Reason, string Kind, string Status, DateTimeOffset SentUtc, string? DeclinedBecause);
