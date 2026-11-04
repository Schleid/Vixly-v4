using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Infrastructure.Texts;

// =========================================================
// FILE: OperationsTwilioGateway.cs
//
// PURPOSE:
// Talks to Twilio's API with the company's own account (Slice 9; owner 2026-10-22).
//
// RESPONSIBILITIES:
// - Check an account; send an SMS (from a number or a messaging service); send a WhatsApp approved template with its variables
//
// IMPORTANT NOTES:
// - Plain HTTPS to api.twilio.com, basic authentication with the account id and token; never logged
// - Logs Twilio's error code only, never the number or the text
// =========================================================
public sealed class OperationsTwilioGateway : IOperationsTextGateway
{
    public const string HttpClientName = "operations-twilio";

    private readonly IHttpClientFactory _clients;

    private readonly ILogger<OperationsTwilioGateway> _logger;

    public OperationsTwilioGateway(IHttpClientFactory clients, ILogger<OperationsTwilioGateway> logger)
    {
        _clients = clients ?? throw new ArgumentNullException(nameof(clients));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> CheckAsync(string accountSid, string authToken, CancellationToken cancellationToken)
    {
        using JsonDocument account = await SendAsync(accountSid, authToken, HttpMethod.Get, "2010-04-01/Accounts/" + Uri.EscapeDataString(accountSid) + ".json", null, cancellationToken);
        return account.RootElement.TryGetProperty("friendly_name", out JsonElement name) && name.ValueKind == JsonValueKind.String ? name.GetString() ?? string.Empty : string.Empty;
    }

    public async Task<string> SendSmsAsync(string accountSid, string authToken, string from, string to, string body, string? statusCallback, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(from);
        List<KeyValuePair<string, string>> form = new() { new("To", to), new("Body", body) };
        form.Add(from.StartsWith("MG", StringComparison.Ordinal) ? new("MessagingServiceSid", from) : new("From", from));
        return await MessageAsync(accountSid, authToken, form, statusCallback, cancellationToken);
    }

    public async Task<string> SendWhatsAppAsync(string accountSid, string authToken, string from, string to, string contentSid, IReadOnlyList<string> variables,
        string? statusCallback, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(variables);
        Dictionary<string, string> values = variables.Select((value, index) => (Key: (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture), value))
            .ToDictionary(item => item.Key, item => item.value);
        List<KeyValuePair<string, string>> form = new()
        {
            new("To", "whatsapp:" + to),
            new("From", "whatsapp:" + from),
            new("ContentSid", contentSid),
            new("ContentVariables", JsonSerializer.Serialize(values))
        };
        return await MessageAsync(accountSid, authToken, form, statusCallback, cancellationToken);
    }

    // Twilio's rule: HMAC-SHA1 with the token over the full address, then each field name and value, names in order (ordinal); base64.
    public bool SignatureValid(string authToken, string url, IReadOnlyList<KeyValuePair<string, string>> form, string? signature)
    {
        ArgumentNullException.ThrowIfNull(form);

        if (string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(url) || string.IsNullOrEmpty(signature))
        {
            return false;
        }

        StringBuilder data = new(url);

        foreach (KeyValuePair<string, string> field in form.OrderBy(item => item.Key, StringComparer.Ordinal).ThenBy(item => item.Value, StringComparer.Ordinal))
        {
            data.Append(field.Key).Append(field.Value);
        }

#pragma warning disable CA5350 // Twilio signs with HMAC-SHA1; the comparison is constant-time.
        byte[] expected = System.Security.Cryptography.HMACSHA1.HashData(Encoding.UTF8.GetBytes(authToken), Encoding.UTF8.GetBytes(data.ToString()));
#pragma warning restore CA5350
        byte[] given;

        try
        {
            given = Convert.FromBase64String(signature);
        }
        catch (FormatException)
        {
            return false;
        }

        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(expected, given);
    }

    private async Task<string> MessageAsync(string accountSid, string authToken, List<KeyValuePair<string, string>> form, string? statusCallback, CancellationToken cancellationToken)
    {
        if (statusCallback is { Length: > 0 })
        {
            form.Add(new("StatusCallback", statusCallback));
        }

        using JsonDocument message = await SendAsync(accountSid, authToken, HttpMethod.Post, "2010-04-01/Accounts/" + Uri.EscapeDataString(accountSid) + "/Messages.json", form,
            cancellationToken);
        return message.RootElement.TryGetProperty("sid", out JsonElement sid) && sid.ValueKind == JsonValueKind.String ? sid.GetString() ?? string.Empty : string.Empty;
    }

    private async Task<JsonDocument> SendAsync(string accountSid, string authToken, HttpMethod method, string path, IList<KeyValuePair<string, string>>? form,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(accountSid) || string.IsNullOrWhiteSpace(authToken))
        {
            throw new OperationsTextRefusedException("No account.");
        }

        using HttpRequestMessage request = new(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(accountSid + ":" + authToken)));

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
            throw new OperationsTextUnavailableException("Twilio could not be reached.", exception);
        }

        using (response)
        {
            string body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                // Accepted is accepted (review): an unreadable answer must not make the message go again.
                try
                {
                    return JsonDocument.Parse(body);
                }
                catch (JsonException)
                {
                    _logger.LogWarning("Twilio answered {Method} with a body that is not JSON.", method.Method);
                    return JsonDocument.Parse("{}");
                }
            }

            if ((int)response.StatusCode >= 500 || response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                throw new OperationsTextUnavailableException("Twilio answered " + (int)response.StatusCode + ".");
            }

            string code = "unknown";

            try
            {
                using JsonDocument error = JsonDocument.Parse(body);

                if (error.RootElement.TryGetProperty("code", out JsonElement value))
                {
                    code = value.ValueKind == JsonValueKind.Number ? value.GetRawText() : value.GetString() ?? code;
                }
            }
            catch (JsonException)
            {
            }

            _logger.LogWarning("Twilio refused {Method}: {Status} {Code}", method.Method, (int)response.StatusCode, code);
            throw new OperationsTextRefusedException("Twilio refused the request: " + code + ".") { Code = code };
        }
    }
}
