namespace Vixly.Operations.Domain.Billing;

// =========================================================
// FILE: OperationsCashDrawerStatus.cs
//
// PURPOSE:
// Whether a cash drawer is in use (Slice 5F, C-4).
//
// RESPONSIBILITIES:
// - Name the states
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// =========================================================
public enum OperationsCashDrawerStatus
{
    Open = 1,
    Closed = 2
}
