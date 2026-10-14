using Microsoft.Extensions.Options;
using Vixly.Platform.Application.Messaging;

namespace Vixly.Platform.Web.Hosting;

// =========================================================
// FILE: PlatformSubscriptionClockWorker.cs
//
// PURPOSE:
// Runs the subscription clock in Platform.Web until the Worker
// opens (Slice 4): trials that ran out, renewals, scheduled
// changes and cancellations that fall due.
//
// RESPONSIBILITIES:
// - At start and then every interval, process due subscriptions
//   in a fresh scope
// - Log failures without stopping
//
// IMPORTANT NOTES:
// - Safe with several web instances: each subscription is saved
//   on its own with a row version, so a second instance loses
//   the race and skips it
// - Disabled with PlatformSubscriptionClock:Enabled = false
// - Moves to Vixly.Platform.Worker unchanged in Slice 4
// =========================================================
public sealed class PlatformSubscriptionClockWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;

    private readonly PlatformSubscriptionClockOptions _options;

    private readonly ILogger<PlatformSubscriptionClockWorker> _logger;

    public PlatformSubscriptionClockWorker(
        IServiceScopeFactory scopes,
        IOptions<PlatformSubscriptionClockOptions> options,
        ILogger<PlatformSubscriptionClockWorker> logger)
    {
        _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        using PeriodicTimer timer = new(TimeSpan.FromMinutes(Math.Max(1, _options.IntervalMinutes)));

        do
        {
            try
            {
                await using AsyncServiceScope scope = _scopes.CreateAsyncScope();
                int processed = await scope.ServiceProvider.GetRequiredService<PlatformSubscriptionClock>().ProcessDueAsync(stoppingToken);

                if (processed > 0)
                {
                    _logger.LogInformation("Subscription clock processed {Count} subscription(s).", processed);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Subscription clock failed; it will run again.");
            }
        }
        while (await NextTickAsync(timer, stoppingToken));
    }

    // Stopping the host cancels the wait: a normal end, not an error.
    private static async Task<bool> NextTickAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return false;
        }
    }
}
