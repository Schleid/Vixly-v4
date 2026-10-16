namespace Vixly.Platform.Domain.Billing;

// =========================================================
// FILE: PlatformCreditNoteStatus.cs
//
// PURPOSE:
// The state of a credit note (D-B6).
//
// RESPONSIBILITIES:
// - Name each value
//
// IMPORTANT NOTES:
// - Persisted as numbers; never renumber
// =========================================================
public enum PlatformCreditNoteStatus
{
    Requested = 0,
    Issued = 1,
    Rejected = 2
}
