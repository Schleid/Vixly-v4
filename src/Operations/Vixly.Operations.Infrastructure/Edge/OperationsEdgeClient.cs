using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vixly.Operations.Application.Branding;
using Vixly.Operations.Application.Ports;
using Vixly.Shared.Kernel.Security;

namespace Vixly.Operations.Infrastructure.Edge;

// =========================================================
// FILE: OperationsEdgeClient.cs
//
// PURPOSE:
// Sends a company's brand to the Cloudflare Worker, signed (Worker design, section 3).
//
// RESPONSIBILITIES:
// - POST the brand as JSON to Edge:BrandUrl with the time and the signature
// - Not configured: nothing sent
// - Any answer but success throws, so the outbox and the nightly round see it
//
// IMPORTANT NOTES:
// - Only https addresses are used
// - Never logs the secret or the body
// =========================================================
internal sealed class OperationsEdgeClient : IOperationsEdgeClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;

    private readonly OperationsEdgeOptions _options;

    private readonly TimeProvider _timeProvider;

    private readonly ILogger<OperationsEdgeClient> _logger;

    public OperationsEdgeClient(HttpClient http, IOptions<OperationsEdgeOptions> options, TimeProvider timeProvider, ILogger<OperationsEdgeClient> logger)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<OperationsEdgeSendResult> SendBrandAsync(OperationsEdgeBrand brand, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(brand);

        if (!Uri.TryCreate(_options.BrandUrl?.Trim(), UriKind.Absolute, out Uri? address) || address.Scheme != Uri.UriSchemeHttps
            || !VixlyEdgeSignature.IsUsable(_options.BrandSecret))
        {
            return OperationsEdgeSendResult.NotConfigured;
        }

        string json = JsonSerializer.Serialize(new
        {
            slug = brand.Slug,
            name = brand.Name,
            colour = brand.Colour,
            language = brand.Language,
            logo = brand.Logo is null ? null : Convert.ToBase64String(brand.Logo),
            logoType = brand.Logo is null ? null : brand.LogoContentType
        }, JsonOptions);

        using HttpRequestMessage request = VixlyEdgeSignature.Request(address, _options.BrandSecret, json, _timeProvider.GetUtcNow());
        using HttpResponseMessage response = await _http.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("The Cloudflare Worker refused the brand of {Slug} with {Status}.", brand.Slug, (int)response.StatusCode);
            throw new HttpRequestException("The Cloudflare Worker refused the brand.", null, response.StatusCode);
        }

        string answer = await response.Content.ReadAsStringAsync(cancellationToken);
        return answer.Contains("\"stale\":true", StringComparison.Ordinal) ? OperationsEdgeSendResult.Stale : OperationsEdgeSendResult.Sent;
    }
}
