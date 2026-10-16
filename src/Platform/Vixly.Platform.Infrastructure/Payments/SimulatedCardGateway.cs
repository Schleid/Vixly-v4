using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vixly.Platform.Application.Payments;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Payments;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Infrastructure.Payments;

// =========================================================
// FILE: SimulatedCardGateway.cs
//
// PURPOSE:
// A pretend card provider for Development, so the whole card flow can be tried without Stripe.
//
// RESPONSIBILITIES:
// - Open a simulated checkout page served by Platform.Api; charge, refund and look up in memory
// - Send signed events to the webhook, like a real provider
// - Cards ending in "declined" are refused; "3ds" asks for the cardholder
//
// IMPORTANT NOTES:
// - Refused outside Development (registration checks the environment)
// - Holds no real money and no card data; state is lost on restart
// - The webhook secret is a development value, not a credential
// =========================================================
public sealed class SimulatedCardGateway : IPlatformCardGateway
{
    public const string SignatureHeader = "Vixly-Simulated-Signature";

    public const string HttpClientName = "vixly-simulated-webhook";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static readonly Action<ILogger, string, Exception?> LogSendFailed =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(52, "SimulatedEventFailed"), "Simulated provider could not deliver event {EventType}");

    private readonly ConcurrentDictionary<string, SimulatedCheckout> _checkouts = new(StringComparer.Ordinal);

    private readonly ConcurrentDictionary<string, PlatformCardResult> _payments = new(StringComparer.Ordinal);

    private readonly PlatformPaymentsOptions _options;

    private readonly IHttpClientFactory _http;

    private readonly ILogger<SimulatedCardGateway> _logger;

    public SimulatedCardGateway(IOptions<PlatformPaymentsOptions> options, string environment, IHttpClientFactory http, ILogger<SimulatedCardGateway> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        bool development = string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase);
        Status = new PlatformCardGatewayStatus(PlatformCardProvider.Simulated, development, PlatformPaymentMode.Test, development ? null : PlatformPaymentsErrors.ModeMismatch.Code);
    }

    public PlatformCardGatewayStatus Status { get; }

    public Task CheckAsync(CancellationToken cancellationToken)
    {
        return Status.Configured ? Task.CompletedTask : throw new PlatformCardGatewayException(PlatformPaymentsErrors.ModeMismatch.Code);
    }

    public Task<string> CreateCustomerAsync(string name, string email, Guid tenantId, string idempotencyKey, CancellationToken cancellationToken)
    {
        return Task.FromResult("sim_cus_" + tenantId.ToString("N"));
    }

    public Task<PlatformCheckoutSession> CreateCheckoutAsync(PlatformCheckoutRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        string id = "sim_cs_" + Hash(request.IdempotencyKey);
        _checkouts.TryAdd(id, new SimulatedCheckout(id, request, "open"));
        return Task.FromResult(new PlatformCheckoutSession(id, _options.SimulatedBaseUrl.TrimEnd('/') + "/dev/simulated-checkout/" + id));
    }

    public SimulatedCheckout? Checkout(string checkoutId) => _checkouts.GetValueOrDefault(checkoutId);

    public Task ExpireCheckoutAsync(string checkoutId, CancellationToken cancellationToken)
    {
        if (_checkouts.TryGetValue(checkoutId, out SimulatedCheckout? checkout) && checkout.Status == "open")
        {
            _checkouts[checkoutId] = checkout with { Status = "expired" };
        }

        return Task.CompletedTask;
    }

    // The simulated checkout page: pay (or save the card) and tell the webhook, like Stripe would.
    public async Task<string> CompleteAsync(string checkoutId, string cardEnding, CancellationToken cancellationToken)
    {
        if (!_checkouts.TryGetValue(checkoutId, out SimulatedCheckout? checkout) || checkout.Status != "open")
        {
            throw new DomainException(PlatformPaymentsErrors.LinkInvalid);
        }

        if (cardEnding.EndsWith("declined", StringComparison.Ordinal))
        {
            return checkout.Request.CancelUrl;
        }

        string method = "sim_pm_" + Hash(checkoutId + cardEnding);
        _checkouts[checkoutId] = checkout with { Status = "complete" };

        if (checkout.Request.Purpose == PlatformCheckoutPurpose.SaveCard)
        {
            await SendAsync(Event(PlatformCardEventKind.CheckoutCompleted, checkoutId, null, null, null, method, checkout.Request.Metadata), cancellationToken);
        }
        else
        {
            string payment = "sim_pi_" + Hash(checkoutId);
            _payments[payment] = new PlatformCardResult(payment, PlatformCardOutcome.Succeeded, null);
            await SendAsync(Event(PlatformCardEventKind.CheckoutCompleted, checkoutId, checkoutId, payment, null, null, checkout.Request.Metadata), cancellationToken);
            await SendAsync(Event(PlatformCardEventKind.PaymentSucceeded, payment, null, payment, null, method, checkout.Request.Metadata), cancellationToken);
        }

        return checkout.Request.SuccessUrl;
    }

    public Task<PlatformCardResult> ChargeAsync(PlatformCardChargeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        string payment = "sim_pi_" + Hash(request.IdempotencyKey);
        PlatformCardResult result = _payments.GetOrAdd(payment, _ => request.PaymentMethodId.EndsWith("declined", StringComparison.Ordinal)
            ? new PlatformCardResult(payment, PlatformCardOutcome.Failed, "card_declined")
            : request.PaymentMethodId.EndsWith("3ds", StringComparison.Ordinal)
                ? new PlatformCardResult(payment, PlatformCardOutcome.RequiresAction, "authentication_required")
                : new PlatformCardResult(payment, PlatformCardOutcome.Succeeded, null));

        if (result.Outcome == PlatformCardOutcome.Succeeded)
        {
            Later(Event(PlatformCardEventKind.PaymentSucceeded, payment, null, payment, null, request.PaymentMethodId, request.Metadata));
        }

        return Task.FromResult(result);
    }

    public Task<PlatformCardResult> RefundAsync(PlatformCardRefundRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        string refund = "sim_re_" + Hash(request.IdempotencyKey);
        Later(Event(PlatformCardEventKind.RefundSucceeded, refund, null, request.PaymentId, refund, null, request.Metadata));
        return Task.FromResult(new PlatformCardResult(refund, PlatformCardOutcome.Pending, null));
    }

    public Task<PlatformCardResult> LookupAsync(string? checkoutId, string? paymentId, CancellationToken cancellationToken)
    {
        if (paymentId is not null && _payments.TryGetValue(paymentId, out PlatformCardResult? known))
        {
            return Task.FromResult(known);
        }

        PlatformCardOutcome outcome = checkoutId is not null && _checkouts.TryGetValue(checkoutId, out SimulatedCheckout? checkout)
            ? checkout.Status switch
            {
                "complete" => PlatformCardOutcome.Succeeded,
                "expired" => PlatformCardOutcome.Cancelled,
                _ => PlatformCardOutcome.Pending
            }
            : PlatformCardOutcome.Pending;
        return Task.FromResult(new PlatformCardResult(checkoutId is null ? paymentId : "sim_pi_" + Hash(checkoutId), outcome, null));
    }

    public Task<PlatformCardDetails> GetCardAsync(string paymentMethodId, CancellationToken cancellationToken)
    {
        return Task.FromResult(new PlatformCardDetails(paymentMethodId, "visa", "4242", 12, DateTime.UtcNow.Year + 3));
    }

    public Task DetachCardAsync(string paymentMethodId, CancellationToken cancellationToken) => Task.CompletedTask;

    public PlatformCardEvent ParseEvent(string payload, string? signature)
    {
        ArgumentNullException.ThrowIfNull(payload);

        byte[] expected = Encoding.ASCII.GetBytes(Sign(payload));
        byte[] given = Encoding.ASCII.GetBytes(signature ?? string.Empty);

        if (!CryptographicOperations.FixedTimeEquals(expected, given))
        {
            throw new DomainException(PlatformPaymentsErrors.EventInvalid);
        }

        try
        {
            return JsonSerializer.Deserialize<PlatformCardEvent>(payload, Json) ?? throw new DomainException(PlatformPaymentsErrors.EventInvalid);
        }
        catch (JsonException)
        {
            throw new DomainException(PlatformPaymentsErrors.EventInvalid);
        }
    }

    private static PlatformCardEvent Event(PlatformCardEventKind kind, string objectId, string? checkoutId, string? paymentId, string? refundId, string? methodId, IReadOnlyDictionary<string, string> metadata)
    {
        return new PlatformCardEvent("sim_evt_" + Guid.NewGuid().ToString("N"), kind.ToString(), kind, objectId, checkoutId, paymentId, refundId, methodId, null, null, null, metadata, DateTimeOffset.UtcNow);
    }

    private void Later(PlatformCardEvent card)
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            await SendAsync(card, CancellationToken.None);
        });
    }

    private async Task SendAsync(PlatformCardEvent card, CancellationToken cancellationToken)
    {
        string payload = JsonSerializer.Serialize(card, Json);

        try
        {
            using HttpRequestMessage message = new(HttpMethod.Post, _options.SimulatedBaseUrl.TrimEnd('/') + "/payments/webhook")
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
            message.Headers.Add(SignatureHeader, Sign(payload));
            using HttpResponseMessage response = await _http.CreateClient(HttpClientName).SendAsync(message, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException exception)
        {
            LogSendFailed(_logger, card.EventType, exception);
        }
    }

    private string Sign(string payload)
    {
        return Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(_options.SimulatedWebhookSecret), Encoding.UTF8.GetBytes(payload)));
    }

    private static string Hash(string value)
    {
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..24];
    }
}
