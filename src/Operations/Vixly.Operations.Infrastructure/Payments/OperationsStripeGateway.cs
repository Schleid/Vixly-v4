using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Infrastructure.Payments;

// =========================================================
// FILE: OperationsStripeGateway.cs
//
// PURPOSE:
// Talks to Stripe's API with the company's own keys (Slice 5F step 4; owner 2026-10-21).
//
// RESPONSIBILITIES:
// - Check a key; make a payment page (Checkout); start a payment on a Terminal reader; read a payment; cancel a reader; refund
// - Check a webhook's signature (Stripe-Signature: HMAC-SHA256 of "t.payload", 5 minutes' tolerance) and read the event
//
// IMPORTANT NOTES:
// - Plain HTTPS to api.stripe.com with a pinned API version; no card data ever passes here (P-4)
// - Keys are sent only in the Authorization header and never logged
// - Every request that makes something carries an idempotency key from our charge's id, so a retry never charges twice
// - Amounts go in the currency's smallest unit (zero-decimal currencies as they are)
// =========================================================
public sealed class OperationsStripeGateway : IOperationsCardGateway
{
    public const string HttpClientName = "operations-stripe";

    public const string ApiVersion = "2024-06-20";

    private const string ChargeKey = "vixly_charge";

    private static readonly TimeSpan Tolerance = TimeSpan.FromMinutes(5);

    private static readonly HashSet<string> ZeroDecimal = new(StringComparer.OrdinalIgnoreCase)
    {
        "BIF", "CLP", "DJF", "GNF", "JPY", "KMF", "KRW", "MGA", "PYG", "RWF", "UGX", "VND", "VUV", "XAF", "XOF", "XPF"
    };

    private static readonly HashSet<string> ThreeDecimal = new(StringComparer.OrdinalIgnoreCase) { "BHD", "JOD", "KWD", "OMR", "TND" };

    private readonly IHttpClientFactory _clients;

    private readonly ILogger<OperationsStripeGateway> _logger;

    public OperationsStripeGateway(IHttpClientFactory clients, ILogger<OperationsStripeGateway> logger)
    {
        _clients = clients ?? throw new ArgumentNullException(nameof(clients));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> CheckAsync(string secretKey, CancellationToken cancellationToken)
    {
        // A restricted key may not read the account: it works, the name stays empty.
        using JsonDocument? account = await SendAsync(secretKey, HttpMethod.Get, "v1/account", null, null, cancellationToken, allowForbidden: true);

        if (account is null)
        {
            return string.Empty;
        }

        JsonElement root = account.RootElement;
        return Text(root, "settings", "dashboard", "display_name") ?? Text(root, "business_profile", "name") ?? string.Empty;
    }

    public async Task<OperationsCardPage> CreatePageAsync(string secretKey, OperationsCardPageRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        string charge = request.ChargeId.ToString("D");
        List<KeyValuePair<string, string>> form = new()
        {
            new("mode", "payment"),
            new("line_items[0][quantity]", "1"),
            new("line_items[0][price_data][currency]", request.CurrencyCode.ToLowerInvariant()),
            new("line_items[0][price_data][unit_amount]", Minor(request.Amount, request.CurrencyCode)),
            new("line_items[0][price_data][product_data][name]", request.Description),
            new("payment_method_types[0]", "card"),
            new("client_reference_id", charge),
            new("metadata[" + ChargeKey + "]", charge),
            new("payment_intent_data[metadata][" + ChargeKey + "]", charge),
            new("payment_intent_data[description]", request.Description),
            new("success_url", request.ReturnUrl ?? throw new ArgumentException("A return address is needed.", nameof(request))),
            new("cancel_url", request.ReturnUrl)
        };

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            form.Add(new("customer_email", request.Email));
        }

        using JsonDocument? session = await SendAsync(secretKey, HttpMethod.Post, "v1/checkout/sessions", form, "page-" + charge, cancellationToken);
        return new OperationsCardPage(Text(session!.RootElement, "id") ?? string.Empty, Text(session.RootElement, "url") ?? string.Empty);
    }

    public async Task<OperationsCardState> GetPageAsync(string secretKey, string checkoutSessionId, CancellationToken cancellationToken)
    {
        using JsonDocument? session = await SendAsync(secretKey, HttpMethod.Get, "v1/checkout/sessions/" + Uri.EscapeDataString(checkoutSessionId), null, null, cancellationToken);
        return Session(session!.RootElement);
    }

    public async Task<string> StartReaderAsync(string secretKey, string readerId, OperationsCardPageRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        string charge = request.ChargeId.ToString("D");
        using JsonDocument? intent = await SendAsync(secretKey, HttpMethod.Post, "v1/payment_intents", new List<KeyValuePair<string, string>>
        {
            new("amount", Minor(request.Amount, request.CurrencyCode)),
            new("currency", request.CurrencyCode.ToLowerInvariant()),
            new("payment_method_types[0]", "card_present"),
            new("capture_method", "automatic"),
            new("description", request.Description),
            new("metadata[" + ChargeKey + "]", charge)
        }, "intent-" + charge, cancellationToken);
        string intentId = Text(intent!.RootElement, "id") ?? throw new OperationsCardRefusedException("No payment returned.");
        using JsonDocument? _ = await SendAsync(secretKey, HttpMethod.Post, "v1/terminal/readers/" + Uri.EscapeDataString(readerId) + "/process_payment_intent",
            new List<KeyValuePair<string, string>> { new("payment_intent", intentId) }, "reader-" + charge, cancellationToken);
        return intentId;
    }

    public async Task<OperationsCardState> GetPaymentAsync(string secretKey, string paymentIntentId, CancellationToken cancellationToken)
    {
        using JsonDocument? intent = await SendAsync(secretKey, HttpMethod.Get, "v1/payment_intents/" + Uri.EscapeDataString(paymentIntentId), null, null, cancellationToken);
        return Intent(intent!.RootElement);
    }

    public async Task CancelReaderAsync(string secretKey, string readerId, CancellationToken cancellationToken)
    {
        using JsonDocument? _ = await SendAsync(secretKey, HttpMethod.Post, "v1/terminal/readers/" + Uri.EscapeDataString(readerId) + "/cancel_action",
            new List<KeyValuePair<string, string>>(), null, cancellationToken);
    }

    public async Task ExpirePageAsync(string secretKey, string checkoutSessionId, CancellationToken cancellationToken)
    {
        using JsonDocument? _ = await SendAsync(secretKey, HttpMethod.Post, "v1/checkout/sessions/" + Uri.EscapeDataString(checkoutSessionId) + "/expire",
            new List<KeyValuePair<string, string>>(), null, cancellationToken);
    }

    public async Task CancelPaymentAsync(string secretKey, string paymentIntentId, CancellationToken cancellationToken)
    {
        using JsonDocument? _ = await SendAsync(secretKey, HttpMethod.Post, "v1/payment_intents/" + Uri.EscapeDataString(paymentIntentId) + "/cancel",
            new List<KeyValuePair<string, string>>(), null, cancellationToken);
    }

    public async Task<string> RefundAsync(string secretKey, string paymentIntentId, decimal amount, string currencyCode, string idempotencyKey, CancellationToken cancellationToken)
    {
        using JsonDocument? refund = await SendAsync(secretKey, HttpMethod.Post, "v1/refunds", new List<KeyValuePair<string, string>>
        {
            new("payment_intent", paymentIntentId),
            new("amount", Minor(amount, currencyCode))
        }, "refund-" + idempotencyKey, cancellationToken);
        return Text(refund!.RootElement, "id") ?? throw new OperationsCardRefusedException("No refund returned.");
    }

    public OperationsCardEvent? ReadEvent(string payload, string? signatureHeader, string webhookSecret, DateTimeOffset utcNow)
    {
        if (string.IsNullOrEmpty(payload) || string.IsNullOrEmpty(signatureHeader) || string.IsNullOrEmpty(webhookSecret) || !Signed(payload, signatureHeader, webhookSecret, utcNow))
        {
            return null;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(payload);
            JsonElement root = document.RootElement;
            string type = Text(root, "type") ?? string.Empty;

            if (!root.TryGetProperty("data", out JsonElement data) || !data.TryGetProperty("object", out JsonElement item))
            {
                return new OperationsCardEvent(type, null, new OperationsCardState(false, false, null, 0m, null));
            }

            Guid? charge = Guid.TryParse(Text(item, "metadata", ChargeKey), out Guid id) ? id : null;
            OperationsCardState state = type switch
            {
                "checkout.session.completed" or "checkout.session.async_payment_succeeded" or "checkout.session.expired" => Session(item),
                "payment_intent.succeeded" or "payment_intent.canceled" => Intent(item),
                _ => new OperationsCardState(false, false, null, 0m, null)
            };
            return new OperationsCardEvent(type, charge, state);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // t=...,v1=... : any v1 matching HMAC-SHA256(secret, "t.payload"), and t within the tolerance.
    public static bool Signed(string payload, string signatureHeader, string webhookSecret, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(signatureHeader);
        string? stamp = null;
        List<string> signatures = new();

        foreach (string part in signatureHeader.Split(','))
        {
            string[] pair = part.Trim().Split('=', 2);

            if (pair.Length == 2 && pair[0] == "t")
            {
                stamp = pair[1];
            }
            else if (pair.Length == 2 && pair[0] == "v1")
            {
                signatures.Add(pair[1]);
            }
        }

        if (stamp is null || !long.TryParse(stamp, NumberStyles.None, CultureInfo.InvariantCulture, out long seconds) || signatures.Count == 0
            || (utcNow - DateTimeOffset.FromUnixTimeSeconds(seconds)).Duration() > Tolerance)
        {
            return false;
        }

        byte[] expected = HMACSHA256.HashData(Encoding.UTF8.GetBytes(webhookSecret), Encoding.UTF8.GetBytes(stamp + "." + payload));
        return signatures.Any(signature => signature.Length == expected.Length * 2 && IsHex(signature)
            && CryptographicOperations.FixedTimeEquals(Convert.FromHexString(signature), expected));
    }

    public static string Minor(decimal amount, string currencyCode)
    {
        ArgumentNullException.ThrowIfNull(currencyCode);
        decimal factor = ZeroDecimal.Contains(currencyCode) ? 1m : ThreeDecimal.Contains(currencyCode) ? 1000m : 100m;
        return decimal.Round(amount * factor, 0, MidpointRounding.AwayFromZero).ToString("0", CultureInfo.InvariantCulture);
    }

    public static decimal Major(long minor, string currencyCode)
    {
        ArgumentNullException.ThrowIfNull(currencyCode);
        decimal factor = ZeroDecimal.Contains(currencyCode) ? 1m : ThreeDecimal.Contains(currencyCode) ? 1000m : 100m;
        return minor / factor;
    }

    private static bool IsHex(string value) => value.Length % 2 == 0 && value.All(char.IsAsciiHexDigit);

    private static OperationsCardState Session(JsonElement session)
    {
        string? currency = Text(session, "currency")?.ToUpperInvariant();
        long total = session.TryGetProperty("amount_total", out JsonElement amount) && amount.ValueKind == JsonValueKind.Number ? amount.GetInt64() : 0;
        bool paid = Text(session, "payment_status") == "paid";
        string? intent = session.TryGetProperty("payment_intent", out JsonElement value) ? value.ValueKind == JsonValueKind.String ? value.GetString() : Text(value, "id") : null;
        return new OperationsCardState(paid, !paid && Text(session, "status") == "expired", intent, currency is null ? 0m : Major(total, currency), currency) { Live = Live(session) };
    }

    private static OperationsCardState Intent(JsonElement intent)
    {
        string? currency = Text(intent, "currency")?.ToUpperInvariant();
        long received = intent.TryGetProperty("amount_received", out JsonElement amount) && amount.ValueKind == JsonValueKind.Number ? amount.GetInt64() : 0;
        string? status = Text(intent, "status");
        return new OperationsCardState(status == "succeeded", status == "canceled", Text(intent, "id"), currency is null ? 0m : Major(received, currency), currency) { Live = Live(intent) };
    }

    private static bool? Live(JsonElement item) =>
        item.TryGetProperty("livemode", out JsonElement live) && live.ValueKind is JsonValueKind.True or JsonValueKind.False ? live.GetBoolean() : null;

    private static string? Text(JsonElement element, params string[] path)
    {
        foreach (string name in path)
        {
            if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out element))
            {
                return null;
            }
        }

        return element.ValueKind == JsonValueKind.String ? element.GetString() : null;
    }

    private async Task<JsonDocument?> SendAsync(string secretKey, HttpMethod method, string path, IList<KeyValuePair<string, string>>? form, string? idempotencyKey,
        CancellationToken cancellationToken, bool allowForbidden = false)
    {
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            throw new OperationsCardRefusedException("No key.");
        }

        using HttpRequestMessage request = new(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secretKey);
        request.Headers.Add("Stripe-Version", ApiVersion);

        if (idempotencyKey is not null)
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        if (form is not null)
        {
            request.Content = new FormUrlEncodedContent(form);
        }

        HttpResponseMessage response;

        try
        {
            response = await _clients.CreateClient(HttpClientName).SendAsync(request, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            throw new OperationsCardUnavailableException("Stripe could not be reached.", exception);
        }

        using (response)
        {
            string body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return JsonDocument.Parse(body);
            }

            if (allowForbidden && response.StatusCode == HttpStatusCode.Forbidden)
            {
                return null;
            }

            if ((int)response.StatusCode >= 500 || response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                throw new OperationsCardUnavailableException("Stripe answered " + (int)response.StatusCode + ".");
            }

            // Stripe's code only (never the key, never the full body): enough to tell what went wrong.
            string code = "unknown";

            try
            {
                using JsonDocument error = JsonDocument.Parse(body);
                code = Text(error.RootElement, "error", "code") ?? Text(error.RootElement, "error", "type") ?? code;
            }
            catch (JsonException)
            {
            }

            _logger.LogWarning("Stripe refused {Method} {Path}: {Status} {Code}", method.Method, path.Split('/')[1], (int)response.StatusCode, code);
            throw new OperationsCardRefusedException("Stripe refused the request: " + code + ".");
        }
    }
}
