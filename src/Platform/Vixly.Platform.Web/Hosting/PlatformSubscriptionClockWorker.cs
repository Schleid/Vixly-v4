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
// - Then mark unpaid invoices past their due date (Billing, D-B8)
//   in a fresh scope
// - Then run the payments clock (Slice 5): automatic charges,
//   waiting refunds, stale card transactions
// - Then collections (Slice 6) and licensing (Slice 7): device
//   requests left unapproved expire
// - Then legal (Slice 8): Terms notices and reminders
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

                // Billing (Slice 4, D-B8): unpaid invoices past their due date.
                await using AsyncServiceScope billing = _scopes.CreateAsyncScope();
                int overdue = await billing.ServiceProvider.GetRequiredService<PlatformBillingClock>().ProcessOverdueAsync(stoppingToken);

                if (overdue > 0)
                {
                    _logger.LogInformation("Billing clock marked {Count} invoice(s) past due.", overdue);
                }

                // Card payments (Slice 5): automatic charges, waiting refunds, stale transactions.
                await using AsyncServiceScope payments = _scopes.CreateAsyncScope();
                int handled = await payments.ServiceProvider.GetRequiredService<PlatformPaymentsClock>().RunAsync(stoppingToken);

                if (handled > 0)
                {
                    _logger.LogInformation("Payments clock handled {Count} card transaction(s).", handled);
                }

                // Collections (Slice 6): reminders, retries, blocks and clearing, one tenant per commit.
                await using AsyncServiceScope collections = _scopes.CreateAsyncScope();
                int checkedTenants = await collections.ServiceProvider.GetRequiredService<PlatformCollectionsClock>().RunAsync(stoppingToken);

                if (checkedTenants > 0)
                {
                    _logger.LogInformation("Collections clock updated {Count} tenant(s).", checkedTenants);
                }

                // Licensing (Slice 7): device requests nobody approved within the limit expire.
                await using AsyncServiceScope licensing = _scopes.CreateAsyncScope();
                int expired = await licensing.ServiceProvider.GetRequiredService<PlatformLicensingClock>().RunAsync(stoppingToken);

                if (expired > 0)
                {
                    _logger.LogInformation("Licensing clock expired {Count} device request(s).", expired);
                }

                // Legal (Slice 8): Terms notices and reminders, each step once per tenant.
                await using AsyncServiceScope legal = _scopes.CreateAsyncScope();
                int reminded = await legal.ServiceProvider.GetRequiredService<PlatformLegalClock>().RunAsync(stoppingToken);

                if (reminded > 0)
                {
                    _logger.LogInformation("Legal clock queued {Count} Terms notice(s).", reminded);
                }

                // Event feed (Slice 9): remove public events past 30 days.
                await using AsyncServiceScope feed = _scopes.CreateAsyncScope();
                int pruned = await feed.ServiceProvider.GetRequiredService<PlatformEventFeedClock>().RunAsync(stoppingToken);

                if (pruned > 0)
                {
                    _logger.LogInformation("Event feed clock removed {Count} old event(s).", pruned);
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
