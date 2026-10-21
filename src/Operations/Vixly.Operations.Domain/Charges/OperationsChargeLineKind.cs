namespace Vixly.Operations.Domain.Charges;

// =========================================================
// FILE: OperationsChargeLineKind.cs
//
// PURPOSE:
// The kinds of line the rating engine writes (Slice 4 design section 6).
//
// RESPONSIBILITIES:
// - Name the choices
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// =========================================================
public enum OperationsChargeLineKind
{
    Freight = 1,
    SmallPackage = 2,
    Item = 3,
    Minimum = 4,
    ServiceFee = 5,
    Consolidation = 6,
    Extra = 7,
    Insurance = 8,

    // A preferred price given by staff on a quote.
    Preferred = 9,

    // Slice 4C, ground: a flat price for the whole shipment.
    ShipmentFreight = 10,

    // Slice 7 step 2 (R-4, R-10): at the destination.
    Duties = 11,
    LocalTax = 12,
    LocalFee = 13,
    Storage = 14
}
