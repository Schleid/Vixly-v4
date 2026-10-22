namespace Vixly.Operations.Domain.Tracking;

// =========================================================
// FILE: OperationsParcelFlags.cs
//
// PURPOSE:
// Flags beside the milestone (ADR-0018 decision 3).
//
// RESPONSIBILITIES:
// - Mark holds, delays, exceptions and inspection state
//
// IMPORTANT NOTES:
// - A hold or a pending inspection blocks bundling, dispatch and handover (ADR-0022)
// =========================================================
[Flags]
public enum OperationsParcelFlags
{
    None = 0,
    OnHold = 1,
    Delayed = 2,
    Exception = 4,
    InspectionPending = 8,
    InspectionInProgress = 16,

    // Received at the dock, not yet checked (condition, weight, size, photos) at the inspection station (Slice 3B).
    AwaitingCheck = 32,

    // Slice 7 (R-3): held by customs at the destination; released by clearance.
    CustomsHeld = 64,

    // Slice 7 step 3 (R-14): its delivery failed too often; it goes back to its origin (staff plan the return).
    ReturnPending = 128
}
