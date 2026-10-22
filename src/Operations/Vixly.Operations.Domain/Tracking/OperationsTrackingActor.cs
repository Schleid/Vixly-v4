namespace Vixly.Operations.Domain.Tracking;

// =========================================================
// FILE: OperationsTrackingActor.cs
//
// PURPOSE:
// Who wrote a tracking event (ADR-0021: who).
//
// RESPONSIBILITIES:
// - Name the kinds of actors
//
// IMPORTANT NOTES:
// - A staff actor also names the user and, when known, the device
// =========================================================
public enum OperationsTrackingActor
{
    Staff = 1,
    System = 2,
    Carrier = 3,
    Partner = 4,
    Customer = 5
}
