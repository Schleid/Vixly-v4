using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.SalesFeed;
using Vixly.Platform.Domain.SalesFeed;

namespace Vixly.Platform.Infrastructure.SalesFeed;

// =========================================================
// FILE: PlatformSalesFeedClient.cs
//
// PURPOSE:
// Posts a batch of sale events to TechOS, signed, and reads the answer (contract version 1).
//
// RESPONSIBILITIES:
// - POST the body to PlatformSales:TechOs:Url with X-TC-Source, X-TC-Contract, X-TC-Timestamp and X-TC-Signature
// - 200: read accepted, duplicates and rejected; 413: too large; 400/401: refused; 404, 5xx, other or none: retry
//
// IMPORTANT NOTES:
// - Off while the URL (https) or the secret is missing
// - Never logs the secret or the body; never throws for an answer or a network failure
// =========================================================
internal sealed class PlatformSalesFeedClient : IPlatformSalesFeedClient
{
    private readonly HttpClient _http;

    private readonly PlatformSalesFeedOptions _options;

    private readonly TimeProvider _timeProvider;

    private readonly ILogger<PlatformSalesFeedClient> _logger;

    public PlatformSalesFeedClient(HttpClient http, IOptions<PlatformSalesFeedOptions> options, TimeProvider timeProvider, ILogger<PlatformSalesFeedClient> logger)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool Configured => SettingsProblem is null;

    // The first setting that stops the feed, named by a stable code the Control page turns into the variable's name.
    public string? SettingsProblem
    {
        get
        {
            string url = _options.Url?.Trim() ?? string.Empty;

            if (url.Length == 0)
            {
                return "url_missing";
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? address) || address.Scheme != Uri.UriSchemeHttps)
            {
                return "url_not_https";
            }

            // TechOS takes the events on its own path (for example /api/sales-sources/vixly/events), never on the site's root.
            if (address.AbsolutePath.Trim('/').Length == 0)
            {
                return "url_no_path";
            }

            if (string.IsNullOrWhiteSpace(_options.Secret))
            {
                return "secret_missing";
            }

            return string.IsNullOrWhiteSpace(_options.Source) ? "source_missing" : null;
        }
    }

    private Uri? Address => Uri.TryCreate(_options.Url?.Trim(), UriKind.Absolute, out Uri? address) && address.Scheme == Uri.UriSchemeHttps ? address : null;

    public async Task<PlatformSalesFeedAnswer> SendAsync(string body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);

        if (!Configured)
        {
            return PlatformSalesFeedAnswer.Of(PlatformSalesFeedAnswerKind.Retry, "not_configured");
        }

        string timestamp = PlatformSalesFeedSignature.Timestamp(_timeProvider.GetUtcNow());
        using HttpRequestMessage request = new(HttpMethod.Post, Address);
        request.Content = new StringContent(body, new UTF8Encoding(false), "application/json");
        request.Headers.Add(PlatformSalesFeedSignature.SourceHeader, _options.Source.Trim());
        request.Headers.Add(PlatformSalesFeedSignature.ContractHeader, PlatformSalesFeedLimits.ContractVersion.ToString(CultureInfo.InvariantCulture));
        request.Headers.Add(PlatformSalesFeedSignature.TimestampHeader, timestamp);
        request.Headers.Add(PlatformSalesFeedSignature.SignatureHeader, PlatformSalesFeedSignature.Sign(_options.Secret.Trim(), timestamp, body));

        try
        {
            using HttpResponseMessage response = await _http.SendAsync(request, cancellationToken);
            string code = "http_" + ((int)response.StatusCode).ToString(CultureInfo.InvariantCulture);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogWarning("TechOS answered the sales feed with {Status}.", (int)response.StatusCode);
            }

            return response.StatusCode switch
            {
                HttpStatusCode.OK => Read(await response.Content.ReadAsStringAsync(cancellationToken), code),
                HttpStatusCode.RequestEntityTooLarge => PlatformSalesFeedAnswer.Of(PlatformSalesFeedAnswerKind.TooLarge, code),
                HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized => PlatformSalesFeedAnswer.Of(PlatformSalesFeedAnswerKind.Refused, code),
                _ => PlatformSalesFeedAnswer.Of(PlatformSalesFeedAnswerKind.Retry, code)
            };
        }
        catch (HttpRequestException)
        {
            _logger.LogWarning("TechOS did not answer the sales feed.");
            return PlatformSalesFeedAnswer.Of(PlatformSalesFeedAnswerKind.Retry, "no_answer");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("TechOS did not answer the sales feed in time.");
            return PlatformSalesFeedAnswer.Of(PlatformSalesFeedAnswerKind.Retry, "no_answer");
        }
    }

    // {"accepted":[ids],"duplicates":[ids],"rejected":[{"id":"…","error":"…"}]}; an unreadable 200 is retried.
    internal static PlatformSalesFeedAnswer Read(string json, string code)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;

            return new PlatformSalesFeedAnswer(
                PlatformSalesFeedAnswerKind.Read,
                code,
                Ids(root, "accepted"),
                Ids(root, "duplicates"),
                root.TryGetProperty("rejected", out JsonElement rejected) && rejected.ValueKind == JsonValueKind.Array
                    ? rejected.EnumerateArray()
                        .Where(item => item.ValueKind == JsonValueKind.Object && item.TryGetProperty("id", out JsonElement id) && id.ValueKind == JsonValueKind.String && Guid.TryParse(id.GetString(), out _))
                        .Select(item => new PlatformSalesFeedRejection(
                            Guid.Parse(item.GetProperty("id").GetString()!),
                            item.TryGetProperty("error", out JsonElement error) && error.ValueKind == JsonValueKind.String ? error.GetString() ?? "rejected" : "rejected"))
                        .ToArray()
                    : Array.Empty<PlatformSalesFeedRejection>());
        }
        catch (JsonException)
        {
            return PlatformSalesFeedAnswer.Of(PlatformSalesFeedAnswerKind.Retry, "unreadable_answer");
        }
    }

    private static IReadOnlyList<Guid> Ids(JsonElement root, string name)
    {
        return root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out JsonElement list) && list.ValueKind == JsonValueKind.Array
            ? list.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.String).Select(item => Guid.TryParse(item.GetString(), out Guid id) ? id : Guid.Empty).Where(id => id != Guid.Empty).ToArray()
            : Array.Empty<Guid>();
    }
}
