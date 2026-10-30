namespace Vixly.Operations.Domain.Inspection;

// =========================================================
// FILE: OperationsRestrictedItemKind.cs
//
// PURPOSE:
// Whether a listed item may not travel at all, or travels with conditions (Slice 3C).
//
// RESPONSIBILITIES:
// - Name the two kinds of entry in a company's restricted-items list
//
// IMPORTANT NOTES:
// - Stored; never renumber
// =========================================================
public enum OperationsRestrictedItemKind
{
    Prohibited = 1,
    Restricted = 2
}
