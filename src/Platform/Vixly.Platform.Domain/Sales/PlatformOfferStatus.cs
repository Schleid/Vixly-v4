namespace Vixly.Platform.Domain.Sales;

// =========================================================
// FILE: PlatformOfferStatus.cs
//
// PURPOSE:
// The state of an offer link (D-S21, D-S24).
//
// RESPONSIBILITIES:
// - Name each value
//
// IMPORTANT NOTES:
// - Persisted as numbers; never renumber
// - Expiry is a date, not a status: an Issued offer past its date is expired
// =========================================================
public enum PlatformOfferStatus
{
    Draft = 0,
    PendingApproval = 1,
    Issued = 2,
    Rejected = 3,
    Used = 4,
    Revoked = 5
}
