using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Vixly.Customer.Web.Portal;

// =========================================================
// FILE: TurnstileCustomerBotCheck.cs
//
// PURPOSE:
// Checks a Cloudflare Turnstile token for customer sign-up (ADR-0019, Slice 2).
//
// RESPONSIBILITIES:
// - Post the token and client address to Turnstile
//
// IMPORTANT NOTES:
// - Any failure counts as "not a person"; off when the check is disabled
// - Only a public client address is sent (a tester inside the network has a private one Cloudflare never saw)
// - A refusal is logged with Cloudflare's error codes, never the token or the secret
// =========================================================
public sealed class TurnstileCustomerBotCheck : ICustomerBotCheck
{
    public const string HttpClientName = "customer-turnstile";

    private readonly IHttpClientFactory _http;

    private readonly CustomerBotCheckOptions _options;

    private readonly ILogger<TurnstileCustomerBotCheck> _logger;

    public TurnstileCustomerBotCheck(IHttpClientFactory http, IOptions<CustomerBotCheckOptions> options, ILogger<TurnstileCustomerBotCheck> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<bool> VerifyAsync(string? token, string? clientIp, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(token) || token.Length > 2048 || string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            return false;
        }

        Dictionary<string, string> form = new(StringComparer.Ordinal) { ["secret"] = _options.SecretKey, ["response"] = token };

        if (IsPublic(clientIp))
        {
            form["remoteip"] = clientIp!;
        }

        try
        {
            using FormUrlEncodedContent content = new(form);
            using HttpResponseMessage response = await _http.CreateClient(HttpClientName).PostAsync(_options.VerifyUrl, content, cancellationToken);
            Answer? answer = response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<Answer>(cancellationToken) : null;

            if (answer?.Success != true)
            {
                _logger.LogWarning("Turnstile refused the token ({Status}): {Codes}.", (int)response.StatusCode, string.Join(", ", answer?.ErrorCodes ?? Array.Empty<string>()));
            }

            return answer?.Success == true;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "Turnstile could not be checked.");
            return false;
        }
    }

    private static bool IsPublic(string? clientIp)
    {
        if (!IPAddress.TryParse(clientIp, out IPAddress? address))
        {
            return false;
        }

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address))
        {
            return false;
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return !address.IsIPv6LinkLocal && !address.IsIPv6SiteLocal && !address.IsIPv6UniqueLocal;
        }

        byte[] b = address.GetAddressBytes();
        return !(b[0] == 10 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168) || (b[0] == 169 && b[1] == 254) || (b[0] == 100 && b[1] >= 64 && b[1] <= 127));
    }

    private sealed record Answer(
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("error-codes")] string[]? ErrorCodes);
}
