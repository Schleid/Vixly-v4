namespace Vixly.Operations.Domain.Charges;

// =========================================================
// FILE: OperationsInsuranceMethod.cs
//
// PURPOSE:
// How the company's insurer charges (Slice 4 design section 5).
//
// RESPONSIBILITIES:
// - Name the choices
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// =========================================================
public enum OperationsInsuranceMethod
{
    // A percentage of the declared value, with a minimum premium.
    Percent = 1,

    // A flat premium for each band of declared value.
    Bands = 2
}
