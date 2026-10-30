using Vixly.Operations.Domain.Inspection;

namespace Vixly.Operations.Application.Inspection;

// =========================================================
// FILE: OperationsStartingItem.cs
//
// PURPOSE:
// One entry of the starting restricted-items list, in one language (Slice 3C).
//
// RESPONSIBILITIES:
// - Carry what the company's copy starts with
//
// IMPORTANT NOTES:
// - Read by CopyOperationsStartingList
// =========================================================
public sealed record OperationsStartingItem(string Name, string Words, OperationsRestrictedItemKind Kind, string Requirement);
