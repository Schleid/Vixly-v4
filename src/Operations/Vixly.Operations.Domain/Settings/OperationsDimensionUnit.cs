namespace Vixly.Operations.Domain.Settings;

// =========================================================
// FILE: OperationsDimensionUnit.cs
//
// PURPOSE:
// The dimension unit a tenant shows and enters (D-O8).
//
// RESPONSIBILITIES:
// - Name the unit; dimensions are stored in centimetres
//
// IMPORTANT NOTES:
// - Stored as int; never renumber
// =========================================================
public enum OperationsDimensionUnit
{
    Centimetre = 1,
    Inch = 2
}
