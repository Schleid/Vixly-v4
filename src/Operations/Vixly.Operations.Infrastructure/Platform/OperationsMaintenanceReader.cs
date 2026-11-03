using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Vixly.Contracts.Platform.Maintenance;

namespace Vixly.Operations.Infrastructure.Platform;

// =========================================================
// FILE: OperationsMaintenanceReader.cs
//
// PURPOSE:
// Reads the maintenance windows Platform publishes for the company websites, staff workspaces and customer portals (Control backlog 2026-10-22, section 1).
//
// RESPONSIBILITIES:
// - GET maintenance?site= and POST maintenance/pass on Platform.Api
//
// IMPORTANT NOTES:
// - Public Platform routes: no service token
// - Platform unreachable: null, so the sites stay open
// =========================================================
public sealed class OperationsMaintenanceReader
{
    private readonly IHttpClientFactory _http;

    private readonly ILogger<OperationsMaintenanceReader> _logger;

    public OperationsMaintenanceReader(IHttpClientFactory http, ILogger<OperationsMaintenanceReader> logger)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<PlatformMaintenanceWindowInfo>?> ReadAsync(string site, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(site);

        try
        {
            return await _http.CreateClient(OperationsPlatformClient.HttpClientName)
                .GetFromJsonAsync<PlatformMaintenanceWindowInfo[]>("maintenance?site=" + Uri.EscapeDataString(site), cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException or InvalidOperationException
            && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "Platform could not be reached for the maintenance windows of {Site}.", site);
            return null;
        }
    }

    public async Task<DateTimeOffset?> CheckPassAsync(string site, string token, CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response = await _http.CreateClient(OperationsPlatformClient.HttpClientName)
                .PostAsJsonAsync("maintenance/pass", new PlatformMaintenancePassBody { Site = site, Token = token }, cancellationToken);

            return response.IsSuccessStatusCode && await response.Content.ReadFromJsonAsync<PlatformMaintenancePassInfo>(cancellationToken) is { } pass
                ? pass.EndsUtc
                : null;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException or InvalidOperationException
            && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "Platform could not be reached to check a maintenance pass.");
            return null;
        }
    }
}
