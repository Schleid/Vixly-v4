namespace Vixly.Operations.Domain.Dispatch;

// =========================================================
// FILE: OperationsDispatchLegStatus.cs
//
// PURPOSE:
// Where a leg of a dispatch stands (Slice 6B, D-4).
//
// RESPONSIBILITIES:
// - Name the states
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// =========================================================
public enum OperationsDispatchLegStatus
{
    Planned = 1,
    Departed = 2,
    Arrived = 3,
    Cancelled = 4
}
