namespace Vixly.Platform.Domain.Billing;

// =========================================================
// FILE: PlatformInvoicePaymentResult.cs
//
// PURPOSE:
// What recording a payment returns: the payment, its history event and whether the invoice became paid.
//
// RESPONSIBILITIES:
// - Carry the changed part and the history event for the same commit
//
// IMPORTANT NOTES:
// - Plain records; no behaviour
// =========================================================
public sealed record PlatformInvoicePaymentResult(PlatformPayment Payment, PlatformInvoiceEvent Event, bool BecamePaid);
