using System.Text.Json;
using Vixly.Platform.Application.Payments;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Payments;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.Tests.Fakes;

// =========================================================
// FILE: FakeCardGateway.cs
//
// PURPOSE:
// A card provider for tests: answers as told and records every call.
//
// RESPONSIBILITIES:
// - Return the next charge, refund and lookup outcome the test chose
// - Accept events whose signature is "valid" (payload: a serialized PlatformCardEvent)
//
// IMPORTANT NOTES:
// - Counts idempotency keys so tests can prove a charge is never sent twice with different keys
// =========================================================
internal sealed class FakeCardGateway : IPlatformCardGateway
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public PlatformCardGatewayStatus Status { get; set; } = new(PlatformCardProvider.Stripe, true, PlatformPaymentMode.Test, null);

    public List<PlatformCheckoutRequest> Checkouts { get; } = new();

    public List<string> Expired { get; } = new();

    public List<PlatformCardChargeRequest> Charges { get; } = new();

    public List<PlatformCardRefundRequest> Refunds { get; } = new();

    public List<string> Detached { get; } = new();

    public PlatformCardResult NextCharge { get; set; } = new("pi_1", PlatformCardOutcome.Succeeded, null);

    public PlatformCardResult NextLookup { get; set; } = new("pi_1", PlatformCardOutcome.Pending, null);

    public bool Unreachable { get; set; }

    public Task CheckAsync(CancellationToken cancellationToken)
    {
        return Unreachable ? throw new PlatformCardGatewayException() : Task.CompletedTask;
    }

    public Task<string> CreateCustomerAsync(string name, string email, Guid tenantId, string idempotencyKey, CancellationToken cancellationToken)
    {
        return Task.FromResult("cus_" + tenantId.ToString("N")[..8]);
    }

    public Task<PlatformCheckoutSession> CreateCheckoutAsync(PlatformCheckoutRequest request, CancellationToken cancellationToken)
    {
        Checkouts.Add(request);
        string id = "cs_" + Checkouts.Count;
        return Task.FromResult(new PlatformCheckoutSession(id, "https://checkout.test/" + id));
    }

    public Task ExpireCheckoutAsync(string checkoutId, CancellationToken cancellationToken)
    {
        Expired.Add(checkoutId);
        return Task.CompletedTask;
    }

    public Task<PlatformCardResult> ChargeAsync(PlatformCardChargeRequest request, CancellationToken cancellationToken)
    {
        if (Unreachable)
        {
            throw new PlatformCardGatewayException();
        }

        Charges.Add(request);
        return Task.FromResult(NextCharge);
    }

    public Task<PlatformCardResult> RefundAsync(PlatformCardRefundRequest request, CancellationToken cancellationToken)
    {
        Refunds.Add(request);
        return Task.FromResult(new PlatformCardResult("re_" + Refunds.Count, PlatformCardOutcome.Pending, null));
    }

    public Task<PlatformCardResult> LookupAsync(string? checkoutId, string? paymentId, CancellationToken cancellationToken) => Task.FromResult(NextLookup);

    public Task<PlatformCardDetails> GetCardAsync(string paymentMethodId, CancellationToken cancellationToken)
    {
        return Task.FromResult(new PlatformCardDetails(paymentMethodId, "visa", "4242", 12, 2030));
    }

    public Task DetachCardAsync(string paymentMethodId, CancellationToken cancellationToken)
    {
        Detached.Add(paymentMethodId);
        return Task.CompletedTask;
    }

    public PlatformCardEvent ParseEvent(string payload, string? signature)
    {
        return signature == "valid"
            ? JsonSerializer.Deserialize<PlatformCardEvent>(payload, Json)!
            : throw new DomainException(PlatformPaymentsErrors.EventInvalid);
    }
}
