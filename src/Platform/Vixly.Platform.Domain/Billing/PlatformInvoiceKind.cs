namespace Vixly.Platform.Domain.Billing;

// =========================================================
// FILE: PlatformInvoiceKind.cs
//
// PURPOSE:
// What an invoice bills (D-B3).
//
// RESPONSIBILITIES:
// - Name each value
//
// IMPORTANT NOTES:
// - Persisted as numbers; never renumber
// =========================================================
public enum PlatformInvoiceKind
{
    FirstPeriod = 1,
    Change = 2,
    Renewal = 3,
    Charges = 4
}
