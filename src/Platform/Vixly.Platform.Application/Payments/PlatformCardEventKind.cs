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
    DisputeClosed = 9
}
