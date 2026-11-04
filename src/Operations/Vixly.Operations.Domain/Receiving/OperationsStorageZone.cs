namespace Vixly.Operations.Domain.Receiving;

// =========================================================
// FILE: OperationsStorageZone.cs
//
// PURPOSE:
// The zones of a warehouse (design section 3).
//
// RESPONSIBILITIES:
// - Name where a parcel may sit
//
// IMPORTANT NOTES:
// - Stored; never renumber
// =========================================================
public enum OperationsStorageZone
{
    Receiving = 1,
    Inspection = 2,
    Storage = 3,
    Hold = 4,
    Outbound = 5,

    // Slice 4D step 3: the undeclared parcels area, where packages nobody can place wait, inventoried (owner, 2026-10-19).
    Undeclared = 6
}
