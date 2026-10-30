using System.Collections.Concurrent;
using Microsoft.Extensions.Logging.Abstractions;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Infrastructure.Payments;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: FakeCardGateway.cs
//
// PURPOSE:
// Stands in for Stripe (Slice 5F step 4): keys, payment pages, readers, payments and refunds kept in memory.
//
// RESPONSIBILITIES:
// - Answer like Stripe for the keys a test accepts
// - Let a test mark a page or a reader payment as paid
// - Read webhook events with the real signature check
//
// IMPORTANT NOTES:
// - Tests only; never reaches the network
// =========================================================
internal sealed class FakeCardGateway : IOperationsCardGateway
{
    private readonly OperationsStripeGateway _real = new(new NoNetwork(), NullLogger<OperationsStripeGateway>.Instance);

    public ConcurrentDictionary<string, OperationsCardState> Payments { get; } = new();

    public ConcurrentDictionary<string, OperationsCardPageRequest> Asked { get; } = new();

    public List<(string Intent, decimal Amount)> Refunds { get; } = new();

    public List<string> ReaderCalls { get; } = new();

    public string AcceptedKeyStart { get; set; } = "sk_test_good";

    // The next refund call times out, as when Stripe cannot be reached.
    public bool RefundUnavailableOnce { get; set; }

    public Task<string> CheckAsync(string secretKey, CancellationToken cancellationToken) =>
        secretKey.StartsWith(AcceptedKeyStart, StringComparison.Ordinal) ? Task.FromResult("Cash Co Stripe") : throw new OperationsCardRefusedException("invalid_api_key");

    public Task<OperationsCardPage> CreatePageAsync(string secretKey, OperationsCardPageRequest request, CancellationToken cancellationToken)
    {
        string id = "cs_test_" + request.ChargeId.ToString("N");
        Asked[id] = request;
        Payments[id] = new OperationsCardState(false, false, null, 0m, null);
        return Task.FromResult(new OperationsCardPage(id, "https://checkout.stripe.com/c/pay/" + id));
    }

    public Task<OperationsCardState> GetPageAsync(string secretKey, string checkoutSessionId, CancellationToken cancellationToken) =>
        Task.FromResult(Payments.TryGetValue(checkoutSessionId, out OperationsCardState? state) ? state : throw new OperationsCardRefusedException("resource_missing"));

    public Task<string> StartReaderAsync(string secretKey, string readerId, OperationsCardPageRequest request, CancellationToken cancellationToken)
    {
        string id = "pi_test_" + request.ChargeId.ToString("N");
        Asked[id] = request;
        Payments[id] = new OperationsCardState(false, false, id, 0m, null);
        ReaderCalls.Add("process:" + readerId);
        return Task.FromResult(id);
    }

    public Task<OperationsCardState> GetPaymentAsync(string secretKey, string paymentIntentId, CancellationToken cancellationToken) =>
        Task.FromResult(Payments.TryGetValue(paymentIntentId, out OperationsCardState? state) ? state : throw new OperationsCardRefusedException("resource_missing"));

    public Task CancelReaderAsync(string secretKey, string readerId, CancellationToken cancellationToken)
    {
        ReaderCalls.Add("cancel:" + readerId);
        return Task.CompletedTask;
    }

    // Like Stripe: an unpaid page or payment is closed; one already paid is refused.
    public Task ExpirePageAsync(string secretKey, string checkoutSessionId, CancellationToken cancellationToken) => Close(checkoutSessionId);

    public Task CancelPaymentAsync(string secretKey, string paymentIntentId, CancellationToken cancellationToken) => Close(paymentIntentId);

    private Task Close(string id)
    {
        if (Payments.TryGetValue(id, out OperationsCardState? state) && state.Paid)
        {
            throw new OperationsCardRefusedException("payment_intent_unexpected_state");
        }

        if (state is not null)
        {
            Payments[id] = state with { Over = true };
        }

        return Task.CompletedTask;
    }

    public Task<string> RefundAsync(string secretKey, string paymentIntentId, decimal amount, string currencyCode, string idempotencyKey, CancellationToken cancellationToken)
    {
        if (RefundUnavailableOnce)
        {
            RefundUnavailableOnce = false;
            throw new OperationsCardUnavailableException();
        }

        Refunds.Add((paymentIntentId, amount));
        return Task.FromResult("re_test_" + Refunds.Count);
    }

    public OperationsCardEvent? ReadEvent(string payload, string? signatureHeader, string webhookSecret, DateTimeOffset utcNow) =>
        _real.ReadEvent(payload, signatureHeader, webhookSecret, utcNow);

    // Stripe took the money for what was asked (a page or a reader payment).
    public void Pay(string id)
    {
        OperationsCardPageRequest asked = Asked[id];
        Payments[id] = new OperationsCardState(true, false, id.StartsWith("pi_", StringComparison.Ordinal) ? id : "pi_test_" + asked.ChargeId.ToString("N"), asked.Amount, asked.CurrencyCode);
    }

    private sealed class NoNetwork : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => throw new InvalidOperationException("Tests never reach Stripe.");
    }
}
