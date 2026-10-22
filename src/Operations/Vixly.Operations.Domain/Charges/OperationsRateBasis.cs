namespace Vixly.Operations.Domain.Charges;

// =========================================================
// FILE: OperationsRateBasis.cs
//
// PURPOSE:
// How a rate card charges the freight of a piece (Slice 4 design section 3).
//
// RESPONSIBILITIES:
// - Name the choices
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// =========================================================
public enum OperationsRateBasis
{
    // A price per lb or kg of billable weight, optionally by weight band.
    Weight = 1,

    // A price per cubic foot (inch companies) or cubic metre (cm companies).
    Volume = 2
}
