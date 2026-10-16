namespace Vixly.Platform.Domain.Payments;

// =========================================================
// FILE: PlatformZelleClaimStatus.cs
//
// PURPOSE:
// Where a customer's "I paid with Zelle" notice stands.
//
// RESPONSIBILITIES:
// - Waiting for staff, confirmed (a payment was recorded) or rejected
//
// IMPORTANT NOTES:
// - Values are stored; never renumber
// =========================================================
public enum PlatformZelleClaimStatus
{
    Waiting = 1,
    Confirmed = 2,
    Rejected = 3
}
