namespace Vixly.Operations.Domain.Receiving;

// =========================================================
// FILE: OperationsPreAlertStatus.cs
//
// PURPOSE:
// State of a pre-alert (Slice 3B).
//
// RESPONSIBILITIES:
// - Open until the package arrives or the customer cancels
//
// IMPORTANT NOTES:
// - Stored; never renumber
// =========================================================
public enum OperationsPreAlertStatus
{
    Open = 0,
    Received = 1,
    Cancelled = 2
}
