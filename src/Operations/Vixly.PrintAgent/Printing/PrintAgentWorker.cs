using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vixly.Contracts.Operations.Printing;

namespace Vixly.PrintAgent.Printing;

// =========================================================
// FILE: PrintAgentWorker.cs
//
// PURPOSE:
// Collects the labels of this warehouse from Operations and prints them (Slice 3B).
//
// RESPONSIBILITIES:
// - Ask Operations for waiting labels every few seconds
// - Send each to its printer and report printed or not
//
// IMPORTANT NOTES:
// - Outbound HTTPS only: nothing listens on the dock computer
// - A refused key stops the program from asking more than once a minute
// - A label is printed at most once and never after it expired; one job failing never stops the others or the service
// =========================================================
public sealed class PrintAgentWorker : BackgroundService
{
    public const string HttpClientName = "operations";

    private readonly IHttpClientFactory _http;

    private readonly PrintAgentOptions _options;

    private readonly Dictionary<string, ILabelPrinterPort> _ports;

    private readonly ILogger<PrintAgentWorker> _logger;

    public PrintAgentWorker(IHttpClientFactory http, IOptions<PrintAgentOptions> options, IEnumerable<ILabelPrinterPort> ports, ILogger<PrintAgentWorker> logger)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _ports = (ports ?? throw new ArgumentNullException(nameof(ports))).ToDictionary(port => port.Connection, StringComparer.Ordinal);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Uri baseAddress = new(_options.OperationsUrl.TrimEnd('/') + "/");
        TimeSpan pause = TimeSpan.FromSeconds(Math.Clamp(_options.PollSeconds, 1, 30));
        _logger.LogInformation("Print agent started for {Host}.", baseAddress.Host);

        while (!stoppingToken.IsCancellationRequested)
        {
            TimeSpan wait = pause;

            try
            {
                HttpClient client = _http.CreateClient(HttpClientName);
                using HttpRequestMessage request = new(HttpMethod.Get, new Uri(baseAddress, "agent/v1/print-jobs"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AgentKey);
                using HttpResponseMessage response = await client.SendAsync(request, stoppingToken);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.LogError("Operations refused the print agent key: register the program again on the Printers page.");
                    wait = TimeSpan.FromMinutes(1);
                }
                else if (response.IsSuccessStatusCode && response.Content.Headers.ContentType?.MediaType == "application/json")
                {
                    System.Diagnostics.Stopwatch received = System.Diagnostics.Stopwatch.StartNew();

                    foreach (PrintJobDto job in await response.Content.ReadFromJsonAsync<List<PrintJobDto>>(stoppingToken) ?? new List<PrintJobDto>())
                    {
                        await PrintAsync(client, baseAddress, job, received.Elapsed, stoppingToken);
                    }
                }
                else
                {
                    _logger.LogWarning("Operations answered {Status}.", (int)response.StatusCode);
                }
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                // A proxy page instead of JSON, a dropped connection, a time-out: wait and ask again, never stop the service.
                _logger.LogWarning("Operations could not be reached: {Error}.", exception.GetType().Name);
                wait = TimeSpan.FromSeconds(10);
            }

            await Task.Delay(wait, stoppingToken);
        }
    }

    private async Task PrintAsync(HttpClient client, Uri baseAddress, PrintJobDto job, TimeSpan waited, CancellationToken cancellationToken)
    {
        string? error;

        try
        {
            error = waited.TotalSeconds >= job.ExpiresInSeconds ? "expired"
                : _ports.TryGetValue(job.Connection, out ILabelPrinterPort? port) ? await port.SendAsync(job.Address, job.Payload, cancellationToken)
                : "connection_unknown";
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            error = "print_failed";
            _logger.LogWarning("Label {Job} failed: {Error}.", job.JobId, exception.GetType().Name);
        }

        if (error is not null)
        {
            _logger.LogWarning("Label {Job} not printed on {Printer}: {Error}.", job.JobId, job.Address, error);
        }

        // The answer is only for the record: a report that does not arrive never holds up the next label.
        try
        {
            using HttpRequestMessage report = new(HttpMethod.Post, new Uri(baseAddress, "agent/v1/print-jobs/" + job.JobId));
            report.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AgentKey);
            report.Content = JsonContent.Create(new PrintJobResultBody(error is null, error));
            using HttpResponseMessage answer = await client.SendAsync(report, cancellationToken);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("The result of label {Job} was not reported: {Error}.", job.JobId, exception.GetType().Name);
        }
    }
}
