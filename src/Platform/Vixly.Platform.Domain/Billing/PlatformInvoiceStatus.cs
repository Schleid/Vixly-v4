namespace Vixly.Platform.Domain.Billing;

// =========================================================
// FILE: PlatformInvoiceStatus.cs
//
// PURPOSE:
// The state of an invoice (D-B6).
//
// RESPONSIBILITIES:
// - Name each value
//
// IMPORTANT NOTES:
// - Persisted as numbers; never renumber
// - Part paid and overdue are computed, not stored
// =========================================================
public enum PlatformInvoiceStatus
{
    Issued = 1,
    Paid = 2,
    Void = 3
}
