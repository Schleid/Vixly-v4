namespace Vixly.Platform.Domain.Billing;

// =========================================================
// FILE: PlatformInvoiceIssue.cs
//
// PURPOSE:
// What issuing an invoice returns: the invoice and its "issued" history event.
//
// RESPONSIBILITIES:
// - Carry the changed part and the history event for the same commit
//
// IMPORTANT NOTES:
// - Plain records; no behaviour
// =========================================================
public sealed record PlatformInvoiceIssue(PlatformInvoice Invoice, PlatformInvoiceEvent Event);
