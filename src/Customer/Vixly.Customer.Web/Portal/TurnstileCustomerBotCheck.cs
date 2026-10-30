using System.Net.Http.Json;
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
// =========================================================
public sealed class TurnstileCustomerBotCheck : ICustomerBotCheck
{
    public const string HttpClientName = "customer-turnstile";

    private readonly IHttpClientFactory _http;

    private readonly CustomerBotCheckOptions _options;

    public TurnstileCustomerBotCheck(IHttpClientFactory http, IOptions<CustomerBotCheckOptions> options)
    {
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

        if (!string.IsNullOrWhiteSpace(clientIp))
        {
            form["remoteip"] = clientIp;
        }

        try
        {
            using FormUrlEncodedContent content = new(form);
            using HttpResponseMessage response = await _http.CreateClient(HttpClientName).PostAsync(_options.VerifyUrl, content, cancellationToken);
            return response.IsSuccessStatusCode && (await response.Content.ReadFromJsonAsync<Answer>(cancellationToken))?.Success == true;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException && !cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    private sealed record Answer([property: JsonPropertyName("success")] bool Success);
}
