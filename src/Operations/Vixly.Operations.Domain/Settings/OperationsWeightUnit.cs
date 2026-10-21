namespace Vixly.Operations.Domain.Settings;

// =========================================================
// FILE: OperationsWeightUnit.cs
//
// PURPOSE:
// The weight unit a tenant shows and enters (D-O8).
//
// RESPONSIBILITIES:
// - Name the unit; weights are stored in kilograms
//
// IMPORTANT NOTES:
// - Stored as int; never renumber
// =========================================================
public enum OperationsWeightUnit
{
    Kilogram = 1,
    Pound = 2
}
