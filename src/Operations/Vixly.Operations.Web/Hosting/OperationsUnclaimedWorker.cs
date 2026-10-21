using Vixly.Operations.Application.Holds;
using Vixly.Operations.Application.Platform;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Holds;
using Vixly.Operations.Domain.Jobs;

namespace Vixly.Operations.Web.Hosting;

// =========================================================
// FILE: OperationsUnclaimedWorker.cs
//
// PURPOSE:
// Sends the reminders of packages waiting for their customer, every hour (Slice 3E, Q-R6).
//
// RESPONSIBILITIES:
// - Find the companies with a package waiting more than a week, in a system scope
// - For each, in its own scope: its portal address from Platform, then the reminders due
// - Record the run (ADR-0019 job runs)
//
// IMPORTANT NOTES:
// - Reminders at day 7, 14 and 21, the final notice at day 25; each package gets each one once
// - The decision list at the company's unclaimed days needs no job: the holds page reads it
// - A failed company is logged and tried again next hour
// =========================================================
public sealed class OperationsUnclaimedWorker : BackgroundService
{
    public const string JobName = "ops.holds.reminders";

    public static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopes;

    private readonly IConfiguration _configuration;

    private readonly TimeProvider _timeProvider;

    private readonly ILogger<OperationsUnclaimedWorker> _logger;

    public OperationsUnclaimedWorker(IServiceScopeFactory scopes, IConfiguration configuration, TimeProvider timeProvider, ILogger<OperationsUnclaimedWorker> logger)
    {
        _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // The portal address of each company comes from Platform.
        if (string.IsNullOrWhiteSpace(_configuration["OperationsPlatform:CallerPrivateKey"]))
        {
            _logger.LogWarning("No Platform caller key is set: package reminders are not sent.");
            return;
        }

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
                _logger.LogError(exception, "Package reminder round failed; it will run again.");
            }
        }
        while (await NextAsync(timer, stoppingToken));
    }

    // One round over every company; public for the tests.
    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        IReadOnlyList<Guid> tenants;

        await using (AsyncServiceScope system = _scopes.CreateAsyncScope())
        {
            system.ServiceProvider.GetRequiredService<IOperationsTenantScope>().EnterSystem();
            tenants = (await system.ServiceProvider.GetRequiredService<IOperationsHoldRepository>().ListTenantsWithWaitingAsync(utcNow.AddDays(-OperationsHoldLimits.ReminderDays[0]), cancellationToken))
                .Union(await system.ServiceProvider.GetRequiredService<IOperationsPickupRepository>().ListTenantsWithRemindableAsync(utcNow, cancellationToken)).ToList();
        }

        int sent = 0;
        int failed = 0;

        foreach (Guid tenantId in tenants)
        {
            try
            {
                await using AsyncServiceScope scope = _scopes.CreateAsyncScope();
                IServiceProvider services = scope.ServiceProvider;

                if (await services.GetRequiredService<IOperationsPlatformClient>().GetTenantAsync(tenantId, cancellationToken) is not { Slug.Length: > 0 } account)
                {
                    continue;
                }

                services.GetRequiredService<IOperationsTenantScope>().Enter(tenantId);
                sent += await services.GetRequiredService<OperationsUnclaimedReminders>().SendAsync(tenantId, account.Slug, utcNow, cancellationToken);

                // Slice 7 step 2 (R-10): shipments waiting at their pickup counter, in their own scope's unit of work.
                await using AsyncServiceScope pickup = _scopes.CreateAsyncScope();
                pickup.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenantId);
                sent += await pickup.ServiceProvider.GetRequiredService<Vixly.Operations.Application.Pickup.OperationsPickupReminders>().SendAsync(tenantId, account.Slug, utcNow, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                failed++;
                _logger.LogError(exception, "Package reminders failed for tenant {TenantId}; they will be tried again.", tenantId);
            }
        }

        await using (AsyncServiceScope record = _scopes.CreateAsyncScope())
        {
            record.ServiceProvider.GetRequiredService<IOperationsTenantScope>().EnterSystem();
            OperationsJobRun run = OperationsJobRun.Start(JobName, utcNow);
            run.Finish(sent, failed, failed > 0 ? "ops.holds.reminders_failed" : null, _timeProvider.GetUtcNow());
            record.ServiceProvider.GetRequiredService<IOperationsJobRunRepository>().Add(run);
            await record.ServiceProvider.GetRequiredService<IOperationsUnitOfWork>().SaveChangesAsync(cancellationToken);
        }

        return sent;
    }

    private static async Task<bool> NextAsync(PeriodicTimer timer, CancellationToken stoppingToken)
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
