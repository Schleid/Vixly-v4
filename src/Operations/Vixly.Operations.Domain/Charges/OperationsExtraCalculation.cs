namespace Vixly.Operations.Domain.Charges;

// =========================================================
// FILE: OperationsExtraCalculation.cs
//
// PURPOSE:
// How an extra charge is worked out (Slice 4 design section 4).
//
// RESPONSIBILITIES:
// - Name the choices
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// =========================================================
public enum OperationsExtraCalculation
{
    Fixed = 1,

    // A percentage of the shipment's freight (after the minimum).
    PercentOfFreight = 2
}
