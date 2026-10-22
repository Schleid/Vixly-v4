using System.Collections.Concurrent;
using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Api.Tests;

// =========================================================
// FILE: FakeCardGateway.cs
//
// PURPOSE:
// Stands in for Stripe on the portal's side (Slice 5F step 4): payment pages kept in memory.
//
// RESPONSIBILITIES:
// - Make a page for what is asked; let a test mark it paid
//
// IMPORTANT NOTES:
// - Tests only; never reaches the network; no webhook here (the check against Stripe records it)
// =========================================================
internal sealed class FakeCardGateway : IOperationsCardGateway
{
    public ConcurrentDictionary<string, OperationsCardPageRequest> Asked { get; } = new();

    public ConcurrentDictionary<string, OperationsCardState> Payments { get; } = new();

    public Task<string> CheckAsync(string secretKey, CancellationToken cancellationToken) => Task.FromResult("Test account");

    public Task<OperationsCardPage> CreatePageAsync(string secretKey, OperationsCardPageRequest request, CancellationToken cancellationToken)
    {
        string id = "cs_test_" + request.ChargeId.ToString("N");
        Asked[id] = request;
        Payments[id] = new OperationsCardState(false, false, null, 0m, null);
        return Task.FromResult(new OperationsCardPage(id, "https://checkout.stripe.com/c/pay/" + id));
    }

    public Task<OperationsCardState> GetPageAsync(string secretKey, string checkoutSessionId, CancellationToken cancellationToken) => Task.FromResult(Payments[checkoutSessionId]);

    public Task<string> StartReaderAsync(string secretKey, string readerId, OperationsCardPageRequest request, CancellationToken cancellationToken) =>
        throw new OperationsCardRefusedException("No readers here.");

    public Task<OperationsCardState> GetPaymentAsync(string secretKey, string paymentIntentId, CancellationToken cancellationToken) => Task.FromResult(Payments[paymentIntentId]);

    public Task CancelReaderAsync(string secretKey, string readerId, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task ExpirePageAsync(string secretKey, string checkoutSessionId, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task CancelPaymentAsync(string secretKey, string paymentIntentId, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<string> RefundAsync(string secretKey, string paymentIntentId, decimal amount, string currencyCode, string idempotencyKey, CancellationToken cancellationToken) =>
        Task.FromResult("re_test_1");

    public OperationsCardEvent? ReadEvent(string payload, string? signatureHeader, string webhookSecret, DateTimeOffset utcNow) => null;

    public void Pay(string id) =>
        Payments[id] = new OperationsCardState(true, false, "pi_test_" + Asked[id].ChargeId.ToString("N"), Asked[id].Amount, Asked[id].CurrencyCode);
}
