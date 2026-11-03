using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Vixly.Contracts.Operations.Printing;

namespace Vixly.PrintHelper.Core;

// =========================================================
// FILE: PrintHelperClient.cs
//
// PURPOSE:
// The print helper's calls to Operations (Slice 12E, owner H-6).
//
// RESPONSIBILITIES:
// - Join a warehouse with the one-time code
// - Save the printer chosen
// - Take the waiting labels and report each result
//
// IMPORTANT NOTES:
// - The same agent API as the warehouse print program (/agent/v1), with the key as a Bearer token
// - Unauthorized means the phone was stopped on the Printers page: the app forgets its key
// =========================================================
public sealed class PrintHelperClient
{
    private readonly HttpClient _http;

    public PrintHelperClient(HttpClient http)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
    }

    public async Task<PhonePairDto?> PairAsync(PairingLink link, string? device, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(link);
        using HttpResponseMessage response = await _http.PostAsJsonAsync(new Uri(link.Site, "agent/v1/pair"), new PhonePairBody(link.Code, device), cancellationToken);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<PhonePairDto>(cancellationToken) : null;
    }

    public async Task<HttpStatusCode> SavePrinterAsync(PrintHelperSettings settings, bool active, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        using HttpRequestMessage request = Request(settings, HttpMethod.Post, "agent/v1/printers");
        request.Content = JsonContent.Create(new PhonePrinterBody(settings.PrinterName ?? string.Empty, settings.PrinterAddress ?? string.Empty, settings.Dpi, settings.Media, active));
        using HttpResponseMessage response = await _http.SendAsync(request, cancellationToken);
        return response.StatusCode;
    }

    public async Task<(HttpStatusCode Status, IReadOnlyList<PrintJobDto> Jobs)> TakeJobsAsync(PrintHelperSettings settings, CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = Request(settings, HttpMethod.Get, "agent/v1/print-jobs");
        using HttpResponseMessage response = await _http.SendAsync(request, cancellationToken);
        IReadOnlyList<PrintJobDto> jobs = response.IsSuccessStatusCode && response.Content.Headers.ContentType?.MediaType == "application/json"
            ? await response.Content.ReadFromJsonAsync<List<PrintJobDto>>(cancellationToken) ?? new List<PrintJobDto>()
            : Array.Empty<PrintJobDto>();
        return (response.StatusCode, jobs);
    }

    public async Task ReportAsync(PrintHelperSettings settings, Guid jobId, string? error, CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = Request(settings, HttpMethod.Post, "agent/v1/print-jobs/" + jobId);
        request.Content = JsonContent.Create(new PrintJobResultBody(error is null, error));
        using HttpResponseMessage response = await _http.SendAsync(request, cancellationToken);
    }

    private static HttpRequestMessage Request(PrintHelperSettings settings, HttpMethod method, string path)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!settings.IsJoined)
        {
            throw new InvalidOperationException("The phone has not joined a warehouse yet.");
        }

        HttpRequestMessage request = new(method, new Uri(settings.Site!, path));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.AgentKey);
        return request;
    }
}
