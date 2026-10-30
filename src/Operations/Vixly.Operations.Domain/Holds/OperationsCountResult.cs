namespace Vixly.Operations.Domain.Holds;

// =========================================================
// FILE: OperationsCountResult.cs
//
// PURPOSE:
// What the clerk found for one parcel of a count of the undeclared parcels area (Slice 4D step 3).
//
// RESPONSIBILITIES:
// - Not counted yet, found on its shelf, missing, or no longer in the area
//
// IMPORTANT NOTES:
// - Stored as a number
// =========================================================
public enum OperationsCountResult
{
    Expected = 0,
    Found = 1,
    Missing = 2,

    // Left the area while the count was open: given to its customer or decided (review of 4D step 3).
    Resolved = 3
}
