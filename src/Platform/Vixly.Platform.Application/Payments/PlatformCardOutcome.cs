namespace Vixly.Platform.Application.Payments;

// =========================================================
// FILE: PlatformCardOutcome.cs
//
// PURPOSE:
// What the card provider answered about a charge or refund.
//
// RESPONSIBILITIES:
// - Carry what the card provider answered about a charge or refund
//
// IMPORTANT NOTES:
// - A synchronous Succeeded only moves the transaction to Pending; the webhook confirms (D-P5)
// =========================================================
public enum PlatformCardOutcome
{
    Pending = 1,
    RequiresAction = 2,
    Succeeded = 3,
    Failed = 4,
    Cancelled = 5
}
