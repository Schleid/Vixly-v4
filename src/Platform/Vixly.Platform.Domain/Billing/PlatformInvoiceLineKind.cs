namespace Vixly.Platform.Domain.Billing;

// =========================================================
// FILE: PlatformInvoiceLineKind.cs
//
// PURPOSE:
// What one invoice line is for.
//
// RESPONSIBILITIES:
// - Name each value
//
// IMPORTANT NOTES:
// - Persisted as numbers; never renumber
// =========================================================
public enum PlatformInvoiceLineKind
{
    PlanPeriod = 1,
    SetupFee = 2,
    ChangeProration = 3,
    OneTimeCharge = 4,
    Discount = 5
}
