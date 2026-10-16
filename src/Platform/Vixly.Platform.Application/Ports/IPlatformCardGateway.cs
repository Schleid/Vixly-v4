using Vixly.Platform.Application.Payments;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformCardGateway.cs
//
// PURPOSE:
// The card provider (Stripe), seen through one small port (D-P1).
//
// RESPONSIBILITIES:
// - Open hosted checkouts, charge saved cards, refund, look up outcomes, read card details
// - Verify webhook signatures and turn events into provider-neutral ones
//
// IMPORTANT NOTES:
// - Card numbers never pass through it (D-P2)
// - Every money call carries an idempotency key
// - Throws PlatformCardGatewayException with a code; never provider text
// =========================================================
public interface IPlatformCardGateway
{
    PlatformCardGatewayStatus Status { get; }

    Task CheckAsync(CancellationToken cancellationToken);

    Task<string> CreateCustomerAsync(string name, string email, Guid tenantId, string idempotencyKey, CancellationToken cancellationToken);

    Task<PlatformCheckoutSession> CreateCheckoutAsync(PlatformCheckoutRequest request, CancellationToken cancellationToken);

    Task ExpireCheckoutAsync(string checkoutId, CancellationToken cancellationToken);

    Task<PlatformCardResult> ChargeAsync(PlatformCardChargeRequest request, CancellationToken cancellationToken);

    Task<PlatformCardResult> RefundAsync(PlatformCardRefundRequest request, CancellationToken cancellationToken);

    // The payment outcome of a checkout or a payment; ProviderId is the payment id.
    Task<PlatformCardResult> LookupAsync(string? checkoutId, string? paymentId, CancellationToken cancellationToken);

    Task<PlatformCardDetails> GetCardAsync(string paymentMethodId, CancellationToken cancellationToken);

    Task DetachCardAsync(string paymentMethodId, CancellationToken cancellationToken);

    // Throws DomainException(platform.payments.event_invalid) when the signature does not match.
    PlatformCardEvent ParseEvent(string payload, string? signature);
}
