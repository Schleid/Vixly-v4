namespace Vixly.Operations.Domain.Inspection;

// =========================================================
// FILE: OperationsInspectionRuleKind.cs
//
// PURPOSE:
// What an inspection rule looks at (Slice 3C design section 3).
//
// RESPONSIBILITIES:
// - Name the four kinds of rule
//
// IMPORTANT NOTES:
// - Stored; never renumber
// =========================================================
public enum OperationsInspectionRuleKind
{
    // The declaration names an item of the company's restricted list (by its words).
    ListedItems = 1,

    // The declared value is at or above an amount.
    ValueAtLeast = 2,

    Carrier = 3,

    DestinationCountry = 4
}
