namespace Vixly.Operations.Domain.Receiving;

// =========================================================
// FILE: OperationsDeliveryStatus.cs
//
// PURPOSE:
// The state of a carrier delivery at the dock (Slice 3B).
//
// RESPONSIBILITIES:
// - Name the states
//
// IMPORTANT NOTES:
// - Values are stored; never renumber
// =========================================================
public enum OperationsDeliveryStatus
{
    Open = 0,
    Closed = 1
}
