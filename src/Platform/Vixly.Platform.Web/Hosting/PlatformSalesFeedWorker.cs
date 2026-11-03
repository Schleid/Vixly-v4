using Microsoft.Extensions.Options;
using Vixly.Platform.Application.Jobs;
using Vixly.Platform.Application.SalesFeed;
using Vixly.Platform.Infrastructure.SalesFeed;

namespace Vixly.Platform.Web.Hosting;

// =========================================================
// FILE: PlatformSalesFeedWorker.cs
//
// PURPOSE:
// Runs the sales feed to TechOS every minute (contract version 1, 2026-11-02).
//
// RESPONSIBILITIES:
// - Run PlatformSalesFeedClock in its own scope on a timer
// - Record a run on the Jobs page when it wrote or sent something, or failed
//
// IMPORTANT NOTES:
// - Section PlatformSales:TechOs (IntervalSeconds, Enabled); off in tests
// - Quiet minutes are not recorded, so the Jobs page is not filled with empty runs
// - A failure is logged and recorded; the next run starts again
// =========================================================
public sealed class PlatformSalesFeedWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;

    private readonly PlatformSalesFeedOptions _options;

    private readonly TimeProvider _timeProvider;

    private readonly ILogger<PlatformSalesFeedWorker> _logger;

    public PlatformSalesFeedWorker(IServiceScopeFactory scopes, IOptions<PlatformSalesFeedOptions> options, TimeProvider timeProvider, ILogger<PlatformSalesFeedWorker> logger)
    {
        _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        using PeriodicTimer timer = new(TimeSpan.FromSeconds(Math.Clamp(_options.IntervalSeconds, 15, 3600)));

        try
        {
            do
            {
                await RunOnceAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Stopping the host ends the loop.
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset startedUtc = _timeProvider.GetUtcNow();
        int handled = 0;
        string? errorCode = null;

        try
        {
            await using AsyncServiceScope scope = _scopes.CreateAsyncScope();
            handled = await scope.ServiceProvider.GetRequiredService<PlatformSalesFeedClock>().RunAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            errorCode = exception.GetType().Name;
            _logger.LogError(exception, "The sales feed run failed; the next run tries again.");
        }

        if (handled == 0 && errorCode is null)
        {
            return;
        }

        try
        {
            await using AsyncServiceScope record = _scopes.CreateAsyncScope();
            await record.ServiceProvider.GetRequiredService<PlatformJobRecorder>().RecordAsync(PlatformJobNames.SalesFeed, startedUtc, handled, errorCode, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "The sales feed run could not be recorded.");
        }
    }
}
