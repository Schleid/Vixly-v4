namespace Vixly.Operations.Application.Charges;

// =========================================================
// FILE: OperationsQuoteState.cs
//
// PURPOSE:
// Where a saved quote stands for the quotes list (owner, 2026-10-20: "a list of previous quotes and their status").
//
// RESPONSIBILITIES:
// - Name the three states the list shows
//
// IMPORTANT NOTES:
// - Expired is not stored: an open quote past its validity date
// =========================================================
public enum OperationsQuoteState
{
    Open = 1,
    Expired = 2,
    Withdrawn = 3
}
