namespace Vixly.Platform.Domain.Collections;

// =========================================================
// FILE: PlatformCollectionCloseReason.cs
//
// PURPOSE:
// Why a collections case closed.
//
// RESPONSIBILITIES:
// - Name the why a collections case closed
//
// IMPORTANT NOTES:
// - Settled: nothing overdue any more (paid, credited or voided)
// - Values are stored; never renumber
// =========================================================
public enum PlatformCollectionCloseReason
{
    Settled = 1,
    SubscriptionEnded = 2,
    TenantClosed = 3
}
