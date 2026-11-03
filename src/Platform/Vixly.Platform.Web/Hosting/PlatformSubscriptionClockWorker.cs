using Microsoft.Extensions.Options;
using Vixly.Platform.Application.Jobs;
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
// - At start and then every interval, run each step in order:
//   subscriptions, billing (D-B8), payments (Slice 5),
//   collections (Slice 6), licensing (Slice 7), legal (Slice 8),
//   the event feed (Slice 9), contact requests, sign-in
//   clean-up, and the job history's own clean-up
// - Run each step in its own scope; a step that fails is logged
//   and recorded, and the next step still runs
// - Record every step's run in JobRuns (owner, 2026-10-23), shown
//   on Control's Jobs page
//
// IMPORTANT NOTES:
// - Safe with several web instances: each subscription is saved
//   on its own with a row version, so a second instance loses
//   the race and skips it
// - Disabled with PlatformSubscriptionClock:Enabled = false
// - Moves to Vixly.Platform.Worker unchanged in Slice 4
// - The run record is written in its own scope, so a step that
//   left its unit of work broken never stops its record
// =========================================================
public sealed class PlatformSubscriptionClockWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;

    private readonly PlatformSubscriptionClockOptions _options;

    private readonly TimeProvider _timeProvider;

    private readonly ILogger<PlatformSubscriptionClockWorker> _logger;

    public PlatformSubscriptionClockWorker(
        IServiceScopeFactory scopes,
        IOptions<PlatformSubscriptionClockOptions> options,
        TimeProvider timeProvider,
        ILogger<PlatformSubscriptionClockWorker> logger)
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

        using PeriodicTimer timer = new(TimeSpan.FromMinutes(Math.Max(1, _options.IntervalMinutes)));

        do
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }
        while (await NextTickAsync(timer, stoppingToken));
    }

    // =====================================================
    // METHOD: RUN ONCE
    // =====================================================
    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        await RunStepAsync(PlatformJobNames.Subscriptions,
            (services, token) => services.GetRequiredService<PlatformSubscriptionClock>().ProcessDueAsync(token), cancellationToken);

        await RunStepAsync(PlatformJobNames.Billing,
            (services, token) => services.GetRequiredService<PlatformBillingClock>().ProcessOverdueAsync(token), cancellationToken);

        await RunStepAsync(PlatformJobNames.Payments,
            (services, token) => services.GetRequiredService<PlatformPaymentsClock>().RunAsync(token), cancellationToken);

        await RunStepAsync(PlatformJobNames.RenewalReminders,
            (services, token) => services.GetRequiredService<PlatformRenewalReminderClock>().RunAsync(token), cancellationToken);

        await RunStepAsync(PlatformJobNames.Collections,
            (services, token) => services.GetRequiredService<PlatformCollectionsClock>().RunAsync(token), cancellationToken);

        await RunStepAsync(PlatformJobNames.Licensing,
            (services, token) => services.GetRequiredService<PlatformLicensingClock>().RunAsync(token), cancellationToken);

        await RunStepAsync(PlatformJobNames.Legal,
            (services, token) => services.GetRequiredService<PlatformLegalClock>().RunAsync(token), cancellationToken);

        await RunStepAsync(PlatformJobNames.EventFeed,
            (services, token) => services.GetRequiredService<PlatformEventFeedClock>().RunAsync(token), cancellationToken);

        await RunStepAsync(PlatformJobNames.Leads,
            (services, token) => services.GetRequiredService<PlatformSalesLeadClock>().RunAsync(token), cancellationToken);

        await RunStepAsync(PlatformJobNames.SignInCleanup,
            (services, token) => services.GetRequiredService<Vixly.Platform.Application.Identity.PlatformSignInCleanupClock>().RunAsync(token), cancellationToken);

        await RunStepAsync(PlatformJobNames.JobRunCleanup,
            (services, token) => services.GetRequiredService<PlatformJobRunClock>().RunAsync(token), cancellationToken);

        await RunStepAsync(PlatformJobNames.EdgeSync,
            (services, token) => services.GetRequiredService<Vixly.Platform.Application.Maintenance.PlatformEdgeMaintenanceClock>().RunAsync(token), cancellationToken);
    }

    // =====================================================
    // METHOD: RUN STEP
    // One step in its own scope, then its record in another.
    // =====================================================
    private async Task RunStepAsync(string job, Func<IServiceProvider, CancellationToken, Task<int>> step, CancellationToken cancellationToken)
    {
        DateTimeOffset startedUtc = _timeProvider.GetUtcNow();
        int handled = 0;
        string? errorCode = null;

        try
        {
            await using AsyncServiceScope scope = _scopes.CreateAsyncScope();
            handled = await step(scope.ServiceProvider, cancellationToken);

            if (handled > 0)
            {
                _logger.LogInformation("Clock step {Job} handled {Count} item(s).", job, handled);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            errorCode = exception.GetType().Name;
            _logger.LogError(exception, "Clock step {Job} failed; the next steps still run.", job);
        }

        try
        {
            await using AsyncServiceScope record = _scopes.CreateAsyncScope();
            await record.ServiceProvider.GetRequiredService<PlatformJobRecorder>().RecordAsync(job, startedUtc, handled, errorCode, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "The run of clock step {Job} could not be recorded.", job);
        }
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
