using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Vixly.Platform.Application.Ports;

namespace Vixly.Platform.Infrastructure.Tenants;

// =========================================================
// FILE: TurnstilePlatformBotCheck.cs
//
// PURPOSE:
// Checks a Cloudflare Turnstile token (D-T12).
//
// RESPONSIBILITIES:
// - Post the token and client IP to Turnstile's siteverify
// - Fail closed on any error or timeout
//
// IMPORTANT NOTES:
// - Enabled with no secret is a configuration error: it fails
//   closed rather than letting bots through
// =========================================================
internal sealed class TurnstilePlatformBotCheck : IPlatformBotCheck
{
    private readonly HttpClient _http;

    private readonly PlatformBotCheckOptions _options;

    public TurnstilePlatformBotCheck(HttpClient http, IOptions<PlatformBotCheckOptions> options)
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

        Dictionary<string, string> form = new(StringComparer.Ordinal)
        {
            ["secret"] = _options.SecretKey,
            ["response"] = token
        };

        if (!string.IsNullOrWhiteSpace(clientIp))
        {
            form["remoteip"] = clientIp;
        }

        try
        {
            using FormUrlEncodedContent content = new(form);
            using HttpResponseMessage response = await _http.PostAsync(_options.VerifyUrl, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            TurnstileAnswer? answer = await response.Content.ReadFromJsonAsync<TurnstileAnswer>(cancellationToken);

            return answer?.Success == true;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }

    private sealed record TurnstileAnswer([property: JsonPropertyName("success")] bool Success);
}
