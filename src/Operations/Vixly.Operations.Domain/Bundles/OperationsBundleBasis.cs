namespace Vixly.Operations.Domain.Bundles;

// =========================================================
// FILE: OperationsBundleBasis.cs
//
// PURPOSE:
// How a bundle's size is judged (Slice 6A, D-2): free fill, or a target in actual or volumetric weight.
//
// RESPONSIBILITIES:
// - Name the bases
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// =========================================================
public enum OperationsBundleBasis
{
    Free = 0,
    Weight = 1,
    Volumetric = 2
}
