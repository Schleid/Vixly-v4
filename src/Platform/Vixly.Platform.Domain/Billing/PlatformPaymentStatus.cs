namespace Vixly.Platform.Domain.Billing;

// =========================================================
// FILE: PlatformPaymentStatus.cs
//
// PURPOSE:
// The state of a recorded payment.
//
// RESPONSIBILITIES:
// - Name each value
//
// IMPORTANT NOTES:
// - Persisted as numbers; never renumber
// =========================================================
public enum PlatformPaymentStatus
{
    Recorded = 1,
    Reversed = 2
}
