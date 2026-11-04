namespace Vixly.Operations.Domain.Charges;

// =========================================================
// FILE: OperationsExtraTrigger.cs
//
// PURPOSE:
// When an extra charge applies (Slice 4 design section 4).
//
// RESPONSIBILITIES:
// - Name the choices
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// =========================================================
public enum OperationsExtraTrigger
{
    Always = 1,

    // The declaration names it (its words, matched like the restricted-items list).
    Declared = 2,

    // Only when staff add it to a quote.
    Manual = 3
}
