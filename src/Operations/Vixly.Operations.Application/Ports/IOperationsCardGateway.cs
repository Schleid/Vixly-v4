namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsCardGateway.cs
//
// PURPOSE:
// Talks to the company's Stripe account with its own keys (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Check the keys; make a payment page; start a payment on a card reader; read a payment; refund
// - Read and check a webhook event with the company's signing secret
//
// IMPORTANT NOTES:
// - Card data never passes through Vixly: cards go to Stripe's page or reader (P-4)
// - Stripe refusing raises OperationsCardRefusedException; Stripe out of reach raises OperationsCardUnavailableException
// - Amounts are in the currency's units (dollars), converted to Stripe's smallest unit by the gateway
// =========================================================
public interface IOperationsCardGateway
{
    // The account's name when the key works.
    Task<string> CheckAsync(string secretKey, CancellationToken cancellationToken);

    Task<OperationsCardPage> CreatePageAsync(string secretKey, OperationsCardPageRequest request, CancellationToken cancellationToken);

    Task<OperationsCardState> GetPageAsync(string secretKey, string checkoutSessionId, CancellationToken cancellationToken);

    // Returns Stripe's payment (pi_...) now waiting on the reader for the customer's card.
    Task<string> StartReaderAsync(string secretKey, string readerId, OperationsCardPageRequest request, CancellationToken cancellationToken);

    Task<OperationsCardState> GetPaymentAsync(string secretKey, string paymentIntentId, CancellationToken cancellationToken);

    Task CancelReaderAsync(string secretKey, string readerId, CancellationToken cancellationToken);

    // Review: a cancelled charge can no longer be paid. Expires the page / cancels the payment; refused when Stripe already took it (then read it).
    Task ExpirePageAsync(string secretKey, string checkoutSessionId, CancellationToken cancellationToken);

    Task CancelPaymentAsync(string secretKey, string paymentIntentId, CancellationToken cancellationToken);

    // Returns Stripe's refund (re_...).
    Task<string> RefundAsync(string secretKey, string paymentIntentId, decimal amount, string currencyCode, string idempotencyKey, CancellationToken cancellationToken);

    // The event when its signature is right and recent; none otherwise.
    OperationsCardEvent? ReadEvent(string payload, string? signatureHeader, string webhookSecret, DateTimeOffset utcNow);
}
