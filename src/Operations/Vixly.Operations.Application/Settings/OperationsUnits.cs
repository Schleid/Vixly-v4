using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Application.Settings;

// =========================================================
// FILE: OperationsUnits.cs
//
// PURPOSE:
// Converts between the tenant's display units and the stored units (D-O8).
//
// RESPONSIBILITIES:
// - Volumetric divisors: cm³ per kg stored, the tenant length³ per weight shown
//
// IMPORTANT NOTES:
// - 1 in = 2.54 cm; 1 lb = 0.45359237 kg
// =========================================================
public static class OperationsUnits
{
    private const decimal CubicCentimetresPerCubicInch = 16.387064m;

    private const decimal KilogramsPerPound = 0.45359237m;

    // How many cm³/kg one divisor unit of the tenant is worth.
    public static decimal DivisorFactor(OperationsDimensionUnit dimension, OperationsWeightUnit weight) =>
        (dimension == OperationsDimensionUnit.Inch ? CubicCentimetresPerCubicInch : 1m) / (weight == OperationsWeightUnit.Pound ? KilogramsPerPound : 1m);

    // Weights and sizes are stored in kilograms and centimetres (D-O8) and shown in the tenant's units.
    public static decimal ToKilograms(decimal shown, OperationsWeightUnit weight) => weight == OperationsWeightUnit.Pound ? shown * KilogramsPerPound : shown;

    public static decimal ToCentimetres(decimal shown, OperationsDimensionUnit dimension) => dimension == OperationsDimensionUnit.Inch ? shown * 2.54m : shown;

    public static decimal ToShownWeight(decimal kilograms, OperationsWeightUnit weight) =>
        decimal.Round(weight == OperationsWeightUnit.Pound ? kilograms / KilogramsPerPound : kilograms, 2, MidpointRounding.AwayFromZero);

    public static decimal ToShownLength(decimal centimetres, OperationsDimensionUnit dimension) =>
        decimal.Round(dimension == OperationsDimensionUnit.Inch ? centimetres / 2.54m : centimetres, 1, MidpointRounding.AwayFromZero);

    // Exact, for the rating engine, which rounds once at the end (Slice 4).
    public static decimal ToShownWeightExact(decimal kilograms, OperationsWeightUnit weight) => weight == OperationsWeightUnit.Pound ? kilograms / KilogramsPerPound : kilograms;

    // Cubic feet for inch companies, cubic metres for cm companies (Slice 4 design section 3).
    public static decimal ToShownVolume(decimal cubicCentimetres, OperationsDimensionUnit dimension) =>
        dimension == OperationsDimensionUnit.Inch ? cubicCentimetres / CubicCentimetresPerCubicInch / 1728m : cubicCentimetres / 1_000_000m;

    public static decimal ToStoredDivisor(decimal shown, OperationsDimensionUnit dimension, OperationsWeightUnit weight) => shown * DivisorFactor(dimension, weight);

    public static decimal ToShownDivisor(decimal stored, OperationsDimensionUnit dimension, OperationsWeightUnit weight) =>
        decimal.Round(stored / DivisorFactor(dimension, weight), 0, MidpointRounding.AwayFromZero);
}
