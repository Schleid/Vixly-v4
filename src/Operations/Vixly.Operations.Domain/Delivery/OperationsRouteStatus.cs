namespace Vixly.Operations.Domain.Delivery;

// =========================================================
// FILE: OperationsRouteStatus.cs
//
// PURPOSE:
// Where a delivery route stands (Slice 7 step 3).
//
// RESPONSIBILITIES:
// - Name the values
//
// IMPORTANT NOTES:
// - Stored as a number; never renumber
// =========================================================
public enum OperationsRouteStatus
{
    Planned = 1,
    Out = 2,
    Returned = 3,
    Settled = 4,
    Cancelled = 5
}
