namespace Vixly.Operations.Domain.Delivery;

// =========================================================
// FILE: OperationsAfterFailedDelivery.cs
//
// PURPOSE:
// What happens after the last failed delivery try (Slice 7 step 3, R-14).
//
// RESPONSIBILITIES:
// - Name the values
//
// IMPORTANT NOTES:
// - Stored as a number; never renumber
// =========================================================
public enum OperationsAfterFailedDelivery
{
    // It waits for pickup at the warehouse; the customer is told.
    WaitForPickup = 1,

    // It goes back to its origin; staff plan the return.
    ReturnToOrigin = 2
}
