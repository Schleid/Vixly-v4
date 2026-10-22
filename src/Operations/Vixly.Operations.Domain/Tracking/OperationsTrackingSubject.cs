namespace Vixly.Operations.Domain.Tracking;

// =========================================================
// FILE: OperationsTrackingSubject.cs
//
// PURPOSE:
// What a tracking event is about (ADR-0021: what).
//
// RESPONSIBILITIES:
// - Name the kinds of subjects
//
// IMPORTANT NOTES:
// - Packages first (Slice 3); bundles and dispatches arrive with Slices 6 and 7
// =========================================================
public enum OperationsTrackingSubject
{
    Parcel = 1,
    PreAlert = 2,
    Receipt = 3,
    Bundle = 4,
    Dispatch = 5,

    // Slice 6C: a customer shipment (manual events).
    Shipment = 6
}
