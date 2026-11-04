namespace Vixly.Operations.Domain.Printing;

// =========================================================
// FILE: OperationsPrintJobStatus.cs
//
// PURPOSE:
// The state of a label waiting for the dock print program (Slice 3B).
//
// RESPONSIBILITIES:
// - Name the states
//
// IMPORTANT NOTES:
// - Values are stored; never renumber
// =========================================================
public enum OperationsPrintJobStatus
{
    Pending = 0,
    Taken = 1,
    Printed = 2,
    Failed = 3
}
