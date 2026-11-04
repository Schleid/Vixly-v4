namespace Vixly.Operations.Domain.Tracking;

// =========================================================
// FILE: OperationsTrackingSource.cs
//
// PURPOSE:
// How a tracking event was captured (ADR-0021: how).
//
// RESPONSIBILITIES:
// - Name the sources
//
// IMPORTANT NOTES:
// - Stored; never renumber
// =========================================================
public enum OperationsTrackingSource
{
    Scan = 1,
    Manual = 2,
    CarrierFeed = 3,
    System = 4,
    Customer = 5,

    // Slice 5C: received at the front-desk counter, with the customer present.
    Counter = 6
}
