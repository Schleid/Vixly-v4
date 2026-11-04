namespace Vixly.Platform.Application.Payments;

// =========================================================
// FILE: PlatformCardEventKind.cs
//
// PURPOSE:
// The provider events Vixly acts on, in provider-neutral terms.
//
// RESPONSIBILITIES:
// - Carry the provider events Vixly acts on, in provider-neutral terms
//
// IMPORTANT NOTES:
// - Each adapter maps its own event types to these
// - Stored inside outbox payloads; never renumber
// - CheckoutProcessing: the checkout completed with a bank debit that has not settled; CheckoutPaymentSucceeded/Failed: that debit's answer
// =========================================================
public enum PlatformCardEventKind
{
    Ignored = 0,
    CheckoutCompleted = 1,
    CheckoutExpired = 2,
    PaymentSucceeded = 3,
    PaymentFailed = 4,
    PaymentRequiresAction = 5,
    RefundSucceeded = 6,
    RefundFailed = 7,
    DisputeOpened = 8,
    DisputeClosed = 9,
    CheckoutProcessing = 10,
    CheckoutPaymentSucceeded = 11,
    CheckoutPaymentFailed = 12
}
