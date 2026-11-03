using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vixly.Platform.Application.Maintenance;
using Vixly.Platform.Application.Ports;
using Vixly.Shared.Kernel.Security;

namespace Vixly.Platform.Infrastructure.Edge;

// =========================================================
// FILE: PlatformEdgeClient.cs
//
// PURPOSE:
// Sends the maintenance windows to the Cloudflare Worker, signed (Worker design, section 2).
//
// RESPONSIBILITIES:
// - POST the whole list as JSON to Edge:MaintenanceUrl with the time and the signature
// - Not configured: nothing sent
// - Any answer but success throws, so the outbox and the Jobs page see it
//
// IMPORTANT NOTES:
// - The body names the windows by id, with site codes, messages and the hash of the pass link; never the link itself
// - Only https addresses are used
// - Never logs the secret or the body
// =========================================================
internal sealed class PlatformEdgeClient : IPlatformEdgeClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;

    private readonly PlatformEdgeOptions _options;

    private readonly TimeProvider _timeProvider;

    private readonly ILogger<PlatformEdgeClient> _logger;

    public PlatformEdgeClient(HttpClient http, IOptions<PlatformEdgeOptions> options, TimeProvider timeProvider, ILogger<PlatformEdgeClient> logger)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PlatformEdgeSendResult> SendMaintenanceAsync(PlatformEdgeWindowList list, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(list);

        if (!Uri.TryCreate(_options.MaintenanceUrl?.Trim(), UriKind.Absolute, out Uri? address) || address.Scheme != Uri.UriSchemeHttps
            || !VixlyEdgeSignature.IsUsable(_options.MaintenanceSecret))
        {
            return PlatformEdgeSendResult.NotConfigured;
        }

        string json = JsonSerializer.Serialize(new
        {
            windows = list.Windows.Select(window => new
            {
                id = window.Id,
                startsUtc = window.StartsUtc.UtcDateTime.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
                endsUtc = window.EndsUtc.UtcDateTime.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
                sites = window.Sites,
                messages = window.Messages,
                passHash = window.PassHash
            })
        }, JsonOptions);

        using HttpRequestMessage request = VixlyEdgeSignature.Request(address, _options.MaintenanceSecret, json, _timeProvider.GetUtcNow());
        using HttpResponseMessage response = await _http.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("The Cloudflare Worker refused the maintenance windows with {Status}.", (int)response.StatusCode);
            throw new HttpRequestException("The Cloudflare Worker refused the maintenance windows.", null, response.StatusCode);
        }

        string answer = await response.Content.ReadAsStringAsync(cancellationToken);
        return answer.Contains("\"stale\":true", StringComparison.Ordinal) ? PlatformEdgeSendResult.Stale : PlatformEdgeSendResult.Sent;
    }
}
