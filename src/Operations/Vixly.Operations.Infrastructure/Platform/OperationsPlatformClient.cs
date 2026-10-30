using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Vixly.Contracts.Platform.Events;
using Vixly.Contracts.Platform.Internal.Portal;
using Vixly.Contracts.Platform.Internal.Tenant;
using Vixly.Operations.Application.Platform;
using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Infrastructure.Platform;

// =========================================================
// FILE: OperationsPlatformClient.cs
//
// PURPOSE:
// Calls Platform.Api for Operations (Stage 3, Slice 0, D-F3).
//
// RESPONSIBILITIES:
// - Look up a host name (scope portal)
// - Read the event feed (scope events)
// - Turn any failure into OperationsPlatformUnavailableException
//
// IMPORTANT NOTES:
// - A fresh signed token on every call; never logged
// - Singleton: takes a client from the factory on each call
// - Platform error bodies are never shown to anyone (S-9); only the status is logged
// =========================================================
public sealed class OperationsPlatformClient : IOperationsPlatformClient
{
    public const string HttpClientName = "operations-platform";

    private readonly IHttpClientFactory _http;

    private readonly OperationsServiceTokenSigner _signer;

    private readonly ILogger<OperationsPlatformClient> _logger;

    public OperationsPlatformClient(IHttpClientFactory http, OperationsServiceTokenSigner signer, ILogger<OperationsPlatformClient> logger)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _signer = signer ?? throw new ArgumentNullException(nameof(signer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<OperationsPortalAnswer> GetPortalAsync(string slug, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        PlatformPortalAccessInfo info = await GetAsync<PlatformPortalAccessInfo>("internal/v1/portal/" + Uri.EscapeDataString(slug), null, cancellationToken);
        return new OperationsPortalAnswer(info.Found, info.TenantId, info.Status, info.Access, info.CanonicalSlug, info.DefaultLanguage, info.Legal);
    }

    public async Task<OperationsPlatformEventPage> ReadEventsAsync(long after, int limit, CancellationToken cancellationToken)
    {
        PlatformEventPageInfo page = await GetAsync<PlatformEventPageInfo>($"internal/v1/events?after={after}&limit={limit}", null, cancellationToken);
        return new OperationsPlatformEventPage(page.Items.Select(item => new OperationsPlatformEvent(item.Sequence, item.Type, item.TenantId)).ToArray(), page.Next, page.More);
    }

    public async Task<OperationsTenantAccount?> GetTenantAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("A tenant id is required.", nameof(tenantId));
        }

        PlatformTenantAccountInfo? info = await GetAsync<PlatformTenantAccountInfo>("internal/v1/tenant", tenantId, cancellationToken, notFoundIsNull: true);

        if (info is null)
        {
            return null;
        }

        PlatformTenantContactInfo? owner = info.Contacts.FirstOrDefault(contact => string.Equals(contact.Role, "owner", StringComparison.OrdinalIgnoreCase));
        return new OperationsTenantAccount(info.TenantId, info.Slug, info.LegalName, info.TradingName, info.DefaultLanguage, owner?.Name, owner?.Email);
    }

    // =====================================================
    // METHOD: GET
    // =====================================================
    private async Task<T> GetAsync<T>(string path, Guid? tenantId, CancellationToken cancellationToken)
        where T : class
    {
        return await GetAsync<T>(path, tenantId, cancellationToken, notFoundIsNull: false)
            ?? throw new OperationsPlatformUnavailableException("Platform sent an empty answer.");
    }

    private async Task<T?> GetAsync<T>(string path, Guid? tenantId, CancellationToken cancellationToken, bool notFoundIsNull)
        where T : class
    {
        try
        {
            using HttpRequestMessage request = new(HttpMethod.Get, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _signer.Sign(tenantId));
            using HttpResponseMessage response = await _http.CreateClient(HttpClientName).SendAsync(request, cancellationToken);

            if (notFoundIsNull && response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Platform answered {Status} to {Path}.", (int)response.StatusCode, path.Split('?')[0]);
                throw new OperationsPlatformUnavailableException("Platform answered " + (int)response.StatusCode + ".");
            }

            return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException or InvalidOperationException
            && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "Platform could not be reached for {Path}.", path.Split('?')[0]);
            throw new OperationsPlatformUnavailableException("Platform could not be reached.", exception);
        }
    }
}
