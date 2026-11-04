namespace Vixly.Operations.Domain.Locations;

// =========================================================
// FILE: OperationsWarehouseRoles.cs
//
// PURPOSE:
// What a warehouse does (sweep D-O10).
//
// RESPONSIBILITIES:
// - Combine receiving, dispatch hub and pickup point
//
// IMPORTANT NOTES:
// - Flags, stored as int; at least one
// =========================================================
[Flags]
public enum OperationsWarehouseRoles
{
    None = 0,
    Receiving = 1,
    DispatchHub = 2,
    PickupPoint = 4,

    // Slice 4B: keeps parcels on its shelves (a location can have every role at once).
    Storage = 8
}
