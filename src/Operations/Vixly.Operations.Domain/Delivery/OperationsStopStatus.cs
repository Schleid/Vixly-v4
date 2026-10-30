namespace Vixly.Operations.Domain.Delivery;

// =========================================================
// FILE: OperationsStopStatus.cs
//
// PURPOSE:
// Where a stop of a route stands (Slice 7 step 3).
//
// RESPONSIBILITIES:
// - Name the values
//
// IMPORTANT NOTES:
// - Stored as a number; never renumber
// =========================================================
public enum OperationsStopStatus
{
    Pending = 1,
    Delivered = 2,
    Failed = 3
}
