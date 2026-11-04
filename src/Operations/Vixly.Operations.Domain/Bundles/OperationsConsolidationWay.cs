namespace Vixly.Operations.Domain.Bundles;

// =========================================================
// FILE: OperationsConsolidationWay.cs
//
// PURPOSE:
// How a consolidated shipment travels (Slice 6A, D-4; owner, A-3).
//
// RESPONSIBILITIES:
// - Name the two ways
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// - The company sets the default; the ship manager may switch it for a shipment
// =========================================================
public enum OperationsConsolidationWay
{
    // Its parcels repacked into one box, weighed and measured, with its own label.
    Repack = 1,

    // Its parcels unchanged, all in one bundle.
    KeepTogether = 2
}
