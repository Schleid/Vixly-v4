namespace Vixly.Operations.Domain.Holds;

// =========================================================
// FILE: OperationsHoldAnswerKind.cs
//
// PURPOSE:
// What a customer sends to answer a hold (Slice 3E, Q-R7).
//
// RESPONSIBILITIES:
// - Name the three kinds of answer
//
// IMPORTANT NOTES:
// - Stored; never renumber
// =========================================================
public enum OperationsHoldAnswerKind
{
    // A document, invoice or permit (PDF, PNG or JPEG).
    Document = 1,

    // The contents and value, confirmed or corrected.
    Value = 2,

    // For a damaged package: ship as is, return to sender, or dispose of.
    DamageChoice = 3
}
