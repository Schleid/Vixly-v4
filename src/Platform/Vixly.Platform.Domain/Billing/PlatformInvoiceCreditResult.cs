namespace Vixly.Platform.Domain.Billing;

// =========================================================
// FILE: PlatformInvoiceCreditResult.cs
//
// PURPOSE:
// What deciding a credit note returns: the credit note and its history event.
//
// RESPONSIBILITIES:
// - Carry the changed part and the history event for the same commit
//
// IMPORTANT NOTES:
// - Plain records; no behaviour
// =========================================================
public sealed record PlatformInvoiceCreditResult(PlatformCreditNote Credit, PlatformInvoiceEvent Event);
