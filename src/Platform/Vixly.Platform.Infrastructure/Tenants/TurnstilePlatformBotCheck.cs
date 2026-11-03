using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
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
// - Only a public client IP is sent: a private or local one
//   (a tester inside the network) never matches the address
//   Cloudflare saw, and Cloudflare refuses the token
// - A refusal is logged with Cloudflare's error codes (never
//   the token or the secret)
// =========================================================
internal sealed class TurnstilePlatformBotCheck : IPlatformBotCheck
{
    private readonly HttpClient _http;

    private readonly PlatformBotCheckOptions _options;

    private readonly ILogger<TurnstilePlatformBotCheck> _logger;

    public TurnstilePlatformBotCheck(HttpClient http, IOptions<PlatformBotCheckOptions> options, ILogger<TurnstilePlatformBotCheck> logger)
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

        Dictionary<string, string> form = new(StringComparer.Ordinal)
        {
            ["secret"] = _options.SecretKey,
            ["response"] = token
        };

        if (IsPublic(clientIp))
        {
            form["remoteip"] = clientIp!;
        }

        try
        {
            using FormUrlEncodedContent content = new(form);
            using HttpResponseMessage response = await _http.PostAsync(_options.VerifyUrl, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Turnstile check answered {Status}.", (int)response.StatusCode);
                return false;
            }

            TurnstileAnswer? answer = await response.Content.ReadFromJsonAsync<TurnstileAnswer>(cancellationToken);

            if (answer?.Success != true)
            {
                _logger.LogWarning("Turnstile refused the token: {Codes}.", string.Join(", ", answer?.ErrorCodes ?? Array.Empty<string>()));
            }

            return answer?.Success == true;
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Turnstile could not be reached.");
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

    private sealed record TurnstileAnswer(
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("error-codes")] string[]? ErrorCodes);
}
