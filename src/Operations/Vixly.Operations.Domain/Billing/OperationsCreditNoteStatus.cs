namespace Vixly.Operations.Domain.Billing;

// =========================================================
// FILE: OperationsCreditNoteStatus.cs
//
// PURPOSE:
// Where a correction stands (Slice 5F, I-6): requested by one person, approved or refused by another.
//
// RESPONSIBILITIES:
// - Name the states
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// =========================================================
public enum OperationsCreditNoteStatus
{
    Requested = 1,
    Approved = 2,
    Refused = 3
}
