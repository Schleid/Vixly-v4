using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using Vixly.Platform.Application.Payments;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Payments;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Infrastructure.Payments;

// =========================================================
// FILE: StripeCardGateway.cs
//
// PURPOSE:
// The card provider port implemented with Stripe (D-P1, D-P2, D-P5).
//
// RESPONSIBILITIES:
// - Open Stripe Checkout for a payment or for saving a card; charge saved cards off-session; refund
// - Look up payments and checkouts; read and detach saved cards
// - Verify webhook signatures and map Stripe events to provider-neutral ones
//
// IMPORTANT NOTES:
// - Card numbers only ever reach Stripe (PCI SAQ A)
// - Every money call sends the transaction's idempotency key
// - Stripe error text is logged without secrets and never returned; callers get codes
// =========================================================
internal sealed class StripeCardGateway : IPlatformCardGateway
{
    private static readonly Action<ILogger, string, string?, Exception?> LogFailed =
        LoggerMessage.Define<string, string?>(LogLevel.Warning, new EventId(51, "StripeCallFailed"), "Stripe call {Operation} failed with code {Code}");

    private readonly PlatformStripeOptions _options;

    private readonly ILogger<StripeCardGateway> _logger;

    private readonly StripeClient? _client;

    public StripeCardGateway(IOptions<PlatformPaymentsOptions> options, string environment, ILogger<StripeCardGateway> logger)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value.Stripe;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Status = PlatformCardModes.Stripe(_options, environment);
        _client = Status.Configured ? new StripeClient(_options.SecretKey.Trim()) : null;
    }

    public PlatformCardGatewayStatus Status { get; }

    private StripeClient Client => _client ?? throw new PlatformCardGatewayException(PlatformPaymentsErrors.NotReady.Code);

    public Task CheckAsync(CancellationToken cancellationToken)
    {
        return CallAsync("check", () => new BalanceService(Client).GetAsync(null, null, cancellationToken));
    }

    public async Task<string> CreateCustomerAsync(string name, string email, Guid tenantId, string idempotencyKey, CancellationToken cancellationToken)
    {
        Customer customer = await CallAsync("customer", () => new CustomerService(Client).CreateAsync(
            new CustomerCreateOptions
            {
                Name = name,
                Email = email,
                Metadata = new Dictionary<string, string> { [PlatformCardMetadata.Tenant] = tenantId.ToString("D") }
            },
            new RequestOptions { IdempotencyKey = idempotencyKey },
            cancellationToken));
        return customer.Id;
    }

    public async Task<PlatformCheckoutSession> CreateCheckoutAsync(PlatformCheckoutRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Dictionary<string, string> metadata = new(request.Metadata, StringComparer.Ordinal);
        SessionCreateOptions options = new()
        {
            Customer = request.CustomerId,
            PaymentMethodTypes = new List<string> { "card" },
            SuccessUrl = request.SuccessUrl,
            CancelUrl = request.CancelUrl,
            Locale = request.Language is "en" or "fr" or "es" ? request.Language : "auto",
            Metadata = metadata,
            ClientReferenceId = metadata.GetValueOrDefault(PlatformCardMetadata.Transaction) ?? metadata.GetValueOrDefault(PlatformCardMetadata.Link)
        };

        if (request.Purpose == PlatformCheckoutPurpose.SaveCard)
        {
            options.Mode = "setup";
            options.SetupIntentData = new SessionSetupIntentDataOptions { Metadata = metadata };
        }
        else
        {
            options.Mode = "payment";
            options.LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = request.CurrencyCode.ToLowerInvariant(),
                        UnitAmount = request.AmountMinor,
                        ProductData = new SessionLineItemPriceDataProductDataOptions { Name = request.Description }
                    }
                }
            };
            options.PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                Description = request.Description,
                Metadata = metadata,
                SetupFutureUsage = request.SaveCard ? "off_session" : null
            };
        }

        Session session = await CallAsync("checkout", () => new SessionService(Client).CreateAsync(options, new RequestOptions { IdempotencyKey = request.IdempotencyKey }, cancellationToken));
        return new PlatformCheckoutSession(session.Id, session.Url);
    }

    public Task ExpireCheckoutAsync(string checkoutId, CancellationToken cancellationToken)
    {
        return CallAsync("expire", async () =>
        {
            Session session = await new SessionService(Client).GetAsync(checkoutId, null, null, cancellationToken);

            return session.Status == "open"
                ? await new SessionService(Client).ExpireAsync(checkoutId, null, null, cancellationToken)
                : session;
        });
    }

    public async Task<PlatformCardResult> ChargeAsync(PlatformCardChargeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            PaymentIntent intent = await new PaymentIntentService(Client).CreateAsync(
                new PaymentIntentCreateOptions
                {
                    Amount = request.AmountMinor,
                    Currency = request.CurrencyCode.ToLowerInvariant(),
                    Customer = request.CustomerId,
                    PaymentMethod = request.PaymentMethodId,
                    OffSession = true,
                    Confirm = true,
                    Description = request.Description,
                    Metadata = new Dictionary<string, string>(request.Metadata, StringComparer.Ordinal)
                },
                new RequestOptions { IdempotencyKey = request.IdempotencyKey },
                cancellationToken);
            return Result(intent);
        }
        catch (StripeException exception) when (exception.StripeError is { } error && (error.Type == "card_error" || error.Code == "authentication_required"))
        {
            // A decline or a bank asking for the cardholder is an answer, not a provider failure.
            return error.Code == "authentication_required"
                ? new PlatformCardResult(error.PaymentIntent?.Id, PlatformCardOutcome.RequiresAction, error.Code)
                : new PlatformCardResult(error.PaymentIntent?.Id, PlatformCardOutcome.Failed, Code(error.DeclineCode ?? error.Code));
        }
        catch (StripeException exception)
        {
            throw Failure("charge", exception);
        }
        catch (HttpRequestException exception)
        {
            throw Failure("charge", exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw Failure("charge", exception);
        }
    }

    public async Task<PlatformCardResult> RefundAsync(PlatformCardRefundRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Refund refund = await CallAsync("refund", () => new RefundService(Client).CreateAsync(
            new RefundCreateOptions
            {
                PaymentIntent = request.PaymentId,
                Amount = request.AmountMinor,
                Metadata = new Dictionary<string, string>(request.Metadata, StringComparer.Ordinal)
            },
            new RequestOptions { IdempotencyKey = request.IdempotencyKey },
            cancellationToken));

        return refund.Status is "failed" or "canceled"
            ? new PlatformCardResult(refund.Id, PlatformCardOutcome.Failed, Code(refund.FailureReason))
            : new PlatformCardResult(refund.Id, PlatformCardOutcome.Pending, null);
    }

    public async Task<PlatformCardResult> LookupAsync(string? checkoutId, string? paymentId, CancellationToken cancellationToken)
    {
        if (paymentId is not null)
        {
            return Result(await CallAsync("lookup", () => new PaymentIntentService(Client).GetAsync(paymentId, null, null, cancellationToken)));
        }

        if (checkoutId is null)
        {
            return new PlatformCardResult(null, PlatformCardOutcome.Pending, null);
        }

        Session session = await CallAsync("lookup", () => new SessionService(Client).GetAsync(checkoutId, null, null, cancellationToken));

        return session.Status switch
        {
            "complete" when session.PaymentStatus == "paid" => new PlatformCardResult(session.PaymentIntentId, PlatformCardOutcome.Succeeded, null),
            "expired" => new PlatformCardResult(session.PaymentIntentId, PlatformCardOutcome.Cancelled, null),
            _ => new PlatformCardResult(session.PaymentIntentId, PlatformCardOutcome.Pending, null)
        };
    }

    public async Task<PlatformCardDetails> GetCardAsync(string paymentMethodId, CancellationToken cancellationToken)
    {
        PaymentMethod method = await CallAsync("card", () => new PaymentMethodService(Client).GetAsync(paymentMethodId, null, null, cancellationToken));

        return method.Card is { } card
            ? new PlatformCardDetails(method.Id, card.Brand, card.Last4, (int)card.ExpMonth, (int)card.ExpYear)
            : throw new DomainException(PlatformPaymentsErrors.CardInvalid);
    }

    public Task DetachCardAsync(string paymentMethodId, CancellationToken cancellationToken)
    {
        return CallAsync("detach", () => new PaymentMethodService(Client).DetachAsync(paymentMethodId, null, null, cancellationToken));
    }

    public PlatformCardEvent ParseEvent(string payload, string? signature)
    {
        Event stripeEvent;

        try
        {
            stripeEvent = EventUtility.ConstructEvent(payload, signature ?? string.Empty, _options.WebhookSecret.Trim(), 300, false);
        }
        catch (StripeException)
        {
            throw new DomainException(PlatformPaymentsErrors.EventInvalid);
        }

        return StripeEvents.Map(stripeEvent);
    }

    private static PlatformCardResult Result(PaymentIntent intent)
    {
        return intent.Status switch
        {
            "succeeded" => new PlatformCardResult(intent.Id, PlatformCardOutcome.Succeeded, null),
            "processing" => new PlatformCardResult(intent.Id, PlatformCardOutcome.Pending, null),
            "requires_action" or "requires_confirmation" => new PlatformCardResult(intent.Id, PlatformCardOutcome.RequiresAction, "authentication_required"),
            "canceled" => new PlatformCardResult(intent.Id, PlatformCardOutcome.Cancelled, null),
            "requires_payment_method" => new PlatformCardResult(intent.Id, PlatformCardOutcome.Failed, Code(intent.LastPaymentError?.DeclineCode ?? intent.LastPaymentError?.Code)),
            _ => new PlatformCardResult(intent.Id, PlatformCardOutcome.Pending, null)
        };
    }

    internal static string? Code(string? code)
    {
        return code is null ? null : PlatformPaymentsLimits.Code(code, PlatformPaymentsLimits.FailureCodeMaxLength);
    }

    private async Task<T> CallAsync<T>(string operation, Func<Task<T>> call)
    {
        try
        {
            return await call();
        }
        catch (StripeException exception)
        {
            throw Failure(operation, exception);
        }
        catch (HttpRequestException exception)
        {
            throw Failure(operation, exception);
        }
        catch (TaskCanceledException exception)
        {
            throw Failure(operation, exception);
        }
    }

    // A refusal by Stripe (4xx) is final; a network error or a 5xx leaves the outcome unknown.
    private PlatformCardGatewayException Failure(string operation, Exception exception)
    {
        string code = exception is StripeException { HttpStatusCode: var status } stripe && (int)status is >= 400 and < 500
            ? PlatformPaymentsErrors.ProviderRefused.Code
            : PlatformPaymentsErrors.ProviderUnavailable.Code;
        LogFailed(_logger, operation, (exception as StripeException)?.StripeError?.Code ?? code, null);
        return new PlatformCardGatewayException(code, exception);
    }
}
