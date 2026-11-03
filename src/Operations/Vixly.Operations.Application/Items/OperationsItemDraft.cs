namespace Vixly.Operations.Application.Items;

// =========================================================
// FILE: OperationsItemDraft.cs
//
// PURPOSE:
// One item as the customer declares it (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - Carry the customer's words, the quantity, the value of one and whether it is used
//
// IMPORTANT NOTES:
// - Validated by the domain when declared
// =========================================================
public sealed record OperationsItemDraft(string? Words, decimal Quantity, decimal ValueEach, bool Used);
