namespace Vixly.Operations.Domain.Charges;

// =========================================================
// FILE: OperationsInsuranceCover.cs
//
// PURPOSE:
// Who pays an insurance claim: the company itself or its insurer (Slice 4C section 8).
//
// RESPONSIBILITIES:
// - Name the two covers
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// =========================================================
public enum OperationsInsuranceCover
{
    // The company pays the claims itself.
    Own = 1,

    // An insurance company, named on the terms with its policy.
    Insurer = 2
}
