namespace Vixly.Operations.Domain.Bundles;

// =========================================================
// FILE: OperationsRepackBoxStatus.cs
//
// PURPOSE:
// Where a repack box stands (Slice 6A, D-4).
//
// RESPONSIBILITIES:
// - Name the states
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// - In a bundle or not is its BundleId
// =========================================================
public enum OperationsRepackBoxStatus
{
    Open = 1,
    Closed = 2,
    Voided = 3
}
