using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Jobs;

namespace Vixly.Operations.Web.Hosting;

// =========================================================
// FILE: OperationsHousekeepingWorker.cs
//
// PURPOSE:
// The hourly clean-up of rows past their keeping date
// (background jobs comparison, 2026-10-22).
//
// RESPONSIBILITIES:
// - Remove expired sign-in rows (OperationsSignInCleanup)
// - Record each run in ops.JobRuns
//
// IMPORTANT NOTES:
// - Runs in the web app on shared hosting (ADR-0007 amendment);
//   a restart or a second copy only finds less to delete
// =========================================================
public sealed class OperationsHousekeepingWorker : BackgroundService
{
    public const string JobName = "ops.housekeeping.sign_in";

    public static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    public const int TrackingBatch = 500;

    private readonly IServiceScopeFactory _scopes;

    private readonly TimeProvider _timeProvider;

    private readonly ILogger<OperationsHousekeepingWorker> _logger;

    public OperationsHousekeepingWorker(IServiceScopeFactory scopes, TimeProvider timeProvider, ILogger<OperationsHousekeepingWorker> logger)
    {
        _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // One round; public for the tests.
    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        int removed = 0;
        string? error = null;

        try
        {
            await using AsyncServiceScope scope = _scopes.CreateAsyncScope();
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().EnterSystem();
            removed = await scope.ServiceProvider.GetRequiredService<OperationsSignInCleanup>().RunAsync(utcNow, cancellationToken);

            // Slice 12C: driver positions are kept 7 days.
            removed += await scope.ServiceProvider.GetRequiredService<IOperationsDriverTrackingRepository>()
                .DeleteOlderThanAsync(utcNow - Vixly.Operations.Domain.Delivery.OperationsDriverPosition.KeptFor, OperationsSignInCleanup.BatchSize, cancellationToken);

            // Owner, 2026-10-28: shipments paid, on account or collect before tracking numbers existed, or missed by a save, get theirs.
            await using AsyncServiceScope tracking = _scopes.CreateAsyncScope();
            tracking.ServiceProvider.GetRequiredService<IOperationsTenantScope>().EnterSystem();
            int given = await tracking.ServiceProvider.GetRequiredService<IOperationsTrackingNumberBackfill>().RunAsync(TrackingBatch, utcNow, cancellationToken);

            if (given > 0)
            {
                _logger.LogInformation("Gave {Count} shipment(s) their tracking number.", given);
            }

            // Owner, 2026-10-31: customer suspensions end by themselves on their end day.
            await using AsyncServiceScope lifts = _scopes.CreateAsyncScope();
            lifts.ServiceProvider.GetRequiredService<IOperationsTenantScope>().EnterSystem();
            int lifted = await lifts.ServiceProvider.GetRequiredService<IOperationsSuspensionLift>().RunAsync(TrackingBatch, utcNow, cancellationToken);

            if (lifted > 0)
            {
                _logger.LogInformation("Lifted {Count} customer suspension(s) that reached their end day.", lifted);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            error = "ops.housekeeping.failed";
            _logger.LogError(exception, "Sign-in clean-up failed; it will run again.");
        }

        await using (AsyncServiceScope record = _scopes.CreateAsyncScope())
        {
            record.ServiceProvider.GetRequiredService<IOperationsTenantScope>().EnterSystem();
            OperationsJobRun run = OperationsJobRun.Start(JobName, utcNow);
            run.Finish(removed, error is null ? 0 : 1, error, _timeProvider.GetUtcNow());
            record.ServiceProvider.GetRequiredService<IOperationsJobRunRepository>().Add(run);
            await record.ServiceProvider.GetRequiredService<IOperationsUnitOfWork>().SaveChangesAsync(cancellationToken);
        }

        if (removed > 0)
        {
            _logger.LogInformation("Sign-in clean-up removed {Count} expired row(s).", removed);
        }

        return removed;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(Interval);

        do
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Housekeeping round failed; it will run again.");
            }
        }
        while (await NextAsync(timer, stoppingToken));
    }

    private static async Task<bool> NextAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
