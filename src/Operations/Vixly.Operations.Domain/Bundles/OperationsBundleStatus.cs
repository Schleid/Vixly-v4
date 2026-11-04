namespace Vixly.Operations.Domain.Bundles;

// =========================================================
// FILE: OperationsBundleStatus.cs
//
// PURPOSE:
// Where a bundle stands (Slice 6A, D-2).
//
// RESPONSIBILITIES:
// - Name the states
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// - Manifested and departed come with dispatch (Slice 6B)
// =========================================================
public enum OperationsBundleStatus
{
    Open = 1,
    Closed = 2,
    Voided = 3
}
