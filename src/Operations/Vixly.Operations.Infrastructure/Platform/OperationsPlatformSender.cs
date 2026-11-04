using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Vixly.Contracts.Platform.Common;
using Vixly.Operations.Application.Platform;
using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Infrastructure.Platform;

// =========================================================
// FILE: OperationsPlatformSender.cs
//
// PURPOSE:
// Sends one signed call to Platform and separates a refusal from an outage (Slice 1C).
//
// RESPONSIBILITIES:
// - Sign for the tenant, and for the acting user when there is one
// - Return the answer, or the refusal code Platform gave
//
// IMPORTANT NOTES:
// - A 4xx with Platform's error body is a refusal; anything else throws OperationsPlatformUnavailableException
// =========================================================
public sealed class OperationsPlatformSender
{
    private readonly IHttpClientFactory _http;

    private readonly OperationsServiceTokenSigner _signer;

    private readonly ILogger<OperationsPlatformSender> _logger;

    public OperationsPlatformSender(IHttpClientFactory http, OperationsServiceTokenSigner signer, ILogger<OperationsPlatformSender> logger)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _signer = signer ?? throw new ArgumentNullException(nameof(signer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<(T? Value, string? RefusedCode)> SendAsync<T>(HttpMethod method, string path, Guid? tenantId, OperationsPlatformActor? actor, object? body, CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            using HttpRequestMessage request = new(method, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _signer.Sign(tenantId, actor));

            if (body is not null)
            {
                request.Content = JsonContent.Create(body, body.GetType());
            }

            using HttpResponseMessage response = await _http.CreateClient(OperationsPlatformClient.HttpClientName).SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return response.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.Accepted || typeof(T) == typeof(object)
                    ? (null, null)
                    : (await response.Content.ReadFromJsonAsync<T>(cancellationToken), null);
            }

            if ((int)response.StatusCode is >= 400 and < 500 && response.StatusCode != HttpStatusCode.Unauthorized
                && await response.Content.ReadFromJsonAsync<PlatformApiError>(cancellationToken) is { } problem)
            {
                return (null, problem.Code);
            }

            _logger.LogWarning("Platform answered {Status} to {Path}.", (int)response.StatusCode, path.Split('?')[0]);
            throw new OperationsPlatformUnavailableException("Platform answered " + (int)response.StatusCode + ".");
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException or NotSupportedException
            && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "Platform could not be reached for {Path}.", path.Split('?')[0]);
            throw new OperationsPlatformUnavailableException("Platform could not be reached.", exception);
        }
    }

    // A file of the tenant (a billing PDF), signed for the acting user; null when Platform does not have it or refuses it.
    public async Task<OperationsLegalFile?> SendFileAsync(string path, Guid tenantId, OperationsPlatformActor actor, CancellationToken cancellationToken)
    {
        try
        {
            using HttpRequestMessage request = new(HttpMethod.Get, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _signer.Sign(tenantId, actor));
            using HttpResponseMessage response = await _http.CreateClient(OperationsPlatformClient.HttpClientName).SendAsync(request, cancellationToken);

            if ((int)response.StatusCode is >= 400 and < 500 && response.StatusCode != HttpStatusCode.Unauthorized)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new OperationsPlatformUnavailableException("Platform answered " + (int)response.StatusCode + ".");
            }

            return new OperationsLegalFile(await response.Content.ReadAsByteArrayAsync(cancellationToken), response.Content.Headers.ContentType?.MediaType ?? "application/pdf");
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            throw new OperationsPlatformUnavailableException("Platform could not be reached.", exception);
        }
    }

    // A public file (a published legal PDF); null when Platform does not have it.
    public async Task<OperationsLegalFile?> GetFileAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response = await _http.CreateClient(OperationsPlatformClient.HttpClientName).GetAsync(path, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new OperationsPlatformUnavailableException("Platform answered " + (int)response.StatusCode + ".");
            }

            return new OperationsLegalFile(await response.Content.ReadAsByteArrayAsync(cancellationToken), response.Content.Headers.ContentType?.MediaType ?? "application/pdf");
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            throw new OperationsPlatformUnavailableException("Platform could not be reached.", exception);
        }
    }
}
