namespace Vixly.Platform.Domain.Billing;

// =========================================================
// FILE: PlatformBillingDocumentKind.cs
//
// PURPOSE:
// The kind of a stored billing document and of its number sequence (D-B2, D-B10).
//
// RESPONSIBILITIES:
// - Name each value
//
// IMPORTANT NOTES:
// - Persisted as numbers; never renumber
// =========================================================
public enum PlatformBillingDocumentKind
{
    Invoice = 1,
    CreditNote = 2,
    Receipt = 3
}
