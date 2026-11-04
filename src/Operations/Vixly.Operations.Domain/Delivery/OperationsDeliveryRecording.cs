namespace Vixly.Operations.Domain.Delivery;

// =========================================================
// FILE: OperationsDeliveryRecording.cs
//
// PURPOSE:
// How drivers record their stops (Slice 7 step 3, R-12).
//
// RESPONSIBILITIES:
// - Name the values
//
// IMPORTANT NOTES:
// - Stored as a number; never renumber
// =========================================================
public enum OperationsDeliveryRecording
{
    // On a phone page, as they go.
    Phone = 1,

    // On the printed run sheet; staff enter the results when the driver is back.
    Sheet = 2
}
