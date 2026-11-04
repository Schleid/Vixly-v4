namespace Vixly.Operations.Domain.Billing;

// =========================================================
// FILE: OperationsCreditNoteKind.cs
//
// PURPOSE:
// A correction of an invoice (Slice 5F, F-6 and I-6).
//
// RESPONSIBILITIES:
// - Name the kinds
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// =========================================================
public enum OperationsCreditNoteKind
{
    // Some lines, all or part of their amount.
    Credit = 1,

    // The whole invoice, while nothing is paid or credited.
    Void = 2
}
