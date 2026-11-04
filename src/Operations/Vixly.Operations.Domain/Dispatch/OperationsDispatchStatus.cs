namespace Vixly.Operations.Domain.Dispatch;

// =========================================================
// FILE: OperationsDispatchStatus.cs
//
// PURPOSE:
// Where a dispatch stands (Slice 6B, D-2).
//
// RESPONSIBILITIES:
// - Name the states
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// - Closed is manifested: nothing more is added
// =========================================================
public enum OperationsDispatchStatus
{
    Planned = 1,
    Closed = 2,
    Departed = 3,
    Arrived = 4,
    Cancelled = 5
}
