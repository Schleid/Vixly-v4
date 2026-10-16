using Stripe;
using Stripe.Checkout;
using Vixly.Platform.Application.Payments;

namespace Vixly.Platform.Infrastructure.Payments;

// =========================================================
// FILE: StripeEvents.cs
//
// PURPOSE:
// Turns a verified Stripe event into a provider-neutral one (D-P5).
//
// RESPONSIBILITIES:
// - Map the Stripe event types Vixly acts on; ignore the rest
// - Carry Vixly's metadata back so the handler finds its transaction or link
//
// IMPORTANT NOTES:
// - Saving a card relies on payment_intent.succeeded and setup_intent.succeeded, which carry the payment method
// =========================================================
internal static class StripeEvents
{
    private static readonly IReadOnlyDictionary<string, string> NoMetadata = new Dictionary<string, string>(StringComparer.Ordinal);

    public static PlatformCardEvent Map(Event stripeEvent)
    {
        ArgumentNullException.ThrowIfNull(stripeEvent);

        DateTimeOffset occurred = new(DateTime.SpecifyKind(stripeEvent.Created, DateTimeKind.Utc));

        switch (stripeEvent.Data.Object)
        {
            case Session session when stripeEvent.Type == "checkout.session.completed" && session.Mode == "payment" && session.PaymentStatus == "paid":
                return New(stripeEvent, PlatformCardEventKind.CheckoutCompleted, session.Id, session.Id, session.PaymentIntentId, null, null, session.CustomerId, null, null, session.Metadata, occurred);
            case Session session when stripeEvent.Type == "checkout.session.expired":
                return New(stripeEvent, PlatformCardEventKind.CheckoutExpired, session.Id, session.Id, session.PaymentIntentId, null, null, session.CustomerId, null, null, session.Metadata, occurred);
            case SetupIntent setup when stripeEvent.Type == "setup_intent.succeeded":
                return New(stripeEvent, PlatformCardEventKind.CheckoutCompleted, setup.Id, null, null, null, setup.PaymentMethodId, setup.CustomerId, null, null, setup.Metadata, occurred);
            case PaymentIntent intent when stripeEvent.Type == "payment_intent.succeeded":
                return New(stripeEvent, PlatformCardEventKind.PaymentSucceeded, intent.Id, null, intent.Id, null, intent.PaymentMethodId, intent.CustomerId, null, null, intent.Metadata, occurred);
            case PaymentIntent intent when stripeEvent.Type == "payment_intent.payment_failed":
                return New(stripeEvent, PlatformCardEventKind.PaymentFailed, intent.Id, null, intent.Id, null, intent.PaymentMethodId, intent.CustomerId,
                    StripeCardGateway.Code(intent.LastPaymentError?.DeclineCode ?? intent.LastPaymentError?.Code ?? "card_declined"), null, intent.Metadata, occurred);
            case PaymentIntent intent when stripeEvent.Type == "payment_intent.requires_action":
                return New(stripeEvent, PlatformCardEventKind.PaymentRequiresAction, intent.Id, null, intent.Id, null, intent.PaymentMethodId, intent.CustomerId, "authentication_required", null, intent.Metadata, occurred);
            case Refund refund when stripeEvent.Type is "refund.created" or "refund.updated" && refund.Status == "succeeded":
                return New(stripeEvent, PlatformCardEventKind.RefundSucceeded, refund.Id, null, refund.PaymentIntentId, refund.Id, null, null, null, null, refund.Metadata, occurred);
            case Refund refund when stripeEvent.Type is "refund.created" or "refund.updated" && refund.Status is "failed" or "canceled":
                return New(stripeEvent, PlatformCardEventKind.RefundFailed, refund.Id, null, refund.PaymentIntentId, refund.Id, null, null, StripeCardGateway.Code(refund.FailureReason ?? refund.Status), null, refund.Metadata, occurred);
            case Dispute dispute when stripeEvent.Type == "charge.dispute.created":
                return New(stripeEvent, PlatformCardEventKind.DisputeOpened, dispute.Id, null, dispute.PaymentIntentId, null, null, null, null, null, dispute.Metadata, occurred);
            case Dispute dispute when stripeEvent.Type == "charge.dispute.closed":
                return New(stripeEvent, PlatformCardEventKind.DisputeClosed, dispute.Id, null, dispute.PaymentIntentId, null, null, null, null, dispute.Status == "won", dispute.Metadata, occurred);
            default:
                return New(stripeEvent, PlatformCardEventKind.Ignored, null, null, null, null, null, null, null, null, null, occurred);
        }
    }

    private static PlatformCardEvent New(
        Event stripeEvent,
        PlatformCardEventKind kind,
        string? objectId,
        string? checkoutId,
        string? paymentId,
        string? refundId,
        string? paymentMethodId,
        string? customerId,
        string? failureCode,
        bool? disputeWon,
        IDictionary<string, string>? metadata,
        DateTimeOffset occurred)
    {
        return new PlatformCardEvent(
            stripeEvent.Id,
            stripeEvent.Type,
            kind,
            objectId,
            checkoutId,
            paymentId,
            refundId,
            paymentMethodId,
            customerId,
            failureCode,
            disputeWon,
            metadata is null ? NoMetadata : new Dictionary<string, string>(metadata, StringComparer.Ordinal),
            occurred);
    }
}
