namespace Vixly.Operations.Domain.Billing;

// =========================================================
// FILE: OperationsTaxKind.cs
//
// PURPOSE:
// The kinds of line a company taxes at its own rate (Slice 5F, F-5 and I-4).
//
// RESPONSIBILITIES:
// - Name the kinds
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// =========================================================
public enum OperationsTaxKind
{
    Freight = 1,
    Insurance = 2,
    Supplies = 3,

    // The shipment's charges and extras: service fee, consolidation, extras.
    Charges = 4
}
