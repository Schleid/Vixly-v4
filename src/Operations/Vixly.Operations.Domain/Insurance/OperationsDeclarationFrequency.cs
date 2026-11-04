namespace Vixly.Operations.Domain.Insurance;

// =========================================================
// FILE: OperationsDeclarationFrequency.cs
//
// PURPOSE:
// How often insured shipments are declared to the insurer under an open policy (Slice 5B).
//
// RESPONSIBILITIES:
// - None, monthly or quarterly
//
// IMPORTANT NOTES:
// - The declaration itself comes in step 3
// =========================================================
public enum OperationsDeclarationFrequency
{
    None = 0,

    Monthly = 1,

    Quarterly = 2
}
