namespace Vixly.Operations.Domain.Charges;

// =========================================================
// FILE: OperationsQuoteStatus.cs
//
// PURPOSE:
// Where a quote stands (Slice 4).
//
// RESPONSIBILITIES:
// - Name the states
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// =========================================================
public enum OperationsQuoteStatus
{
    Open = 1,
    Withdrawn = 2
}
