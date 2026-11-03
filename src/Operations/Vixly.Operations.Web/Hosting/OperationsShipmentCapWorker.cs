using Vixly.Operations.Application.Plans;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Jobs;

namespace Vixly.Operations.Web.Hosting;

// =========================================================
// FILE: OperationsShipmentCapWorker.cs
//
// PURPOSE:
// Every hour: each company's shipments to Platform, and the plan warnings to the Owner (shipment cap, owner 2026-10-29).
//
// RESPONSIBILITIES:
// - Find the companies that created shipments in the last two months
// - Run OperationsShipmentCapReport for each, in its own scope
// - Record the round in the job history (ops.plans.shipment_cap)
//
// IMPORTANT NOTES:
// - Without the Platform caller key nothing is sent (development without Platform)
// - One company failing never stops the others
// =========================================================
public sealed class OperationsShipmentCapWorker : BackgroundService
{
    public const string JobName = "ops.plans.shipment_cap";

    public static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopes;

    private readonly IConfiguration _configuration;

    private readonly TimeProvider _timeProvider;

    private readonly ILogger<OperationsShipmentCapWorker> _logger;

    public OperationsShipmentCapWorker(IServiceScopeFactory scopes, IConfiguration configuration, TimeProvider timeProvider, ILogger<OperationsShipmentCapWorker> logger)
    {
        _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_configuration["OperationsPlatform:CallerPrivateKey"]))
        {
            _logger.LogWarning("No Platform caller key is set: shipment counts are not sent.");
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
                _logger.LogError(exception, "Shipment cap round failed; it will run again.");
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
            tenants = await system.ServiceProvider.GetRequiredService<IOperationsShipmentCounter>().ListTenantsCreatingSinceAsync(utcNow.AddDays(-62), cancellationToken);
        }

        int warned = 0;
        int failed = 0;

        foreach (Guid tenantId in tenants)
        {
            try
            {
                await using AsyncServiceScope scope = _scopes.CreateAsyncScope();
                scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenantId);
                warned += await scope.ServiceProvider.GetRequiredService<OperationsShipmentCapReport>().RunAsync(tenantId, utcNow, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                failed++;
                _logger.LogError(exception, "Shipment cap round failed for tenant {TenantId}; it will be tried again.", tenantId);
            }
        }

        await using (AsyncServiceScope record = _scopes.CreateAsyncScope())
        {
            record.ServiceProvider.GetRequiredService<IOperationsTenantScope>().EnterSystem();
            OperationsJobRun run = OperationsJobRun.Start(JobName, utcNow);
            run.Finish(tenants.Count, failed, failed > 0 ? "ops.plans.shipment_cap_failed" : null, _timeProvider.GetUtcNow());
            record.ServiceProvider.GetRequiredService<IOperationsJobRunRepository>().Add(run);
            await record.ServiceProvider.GetRequiredService<IOperationsUnitOfWork>().SaveChangesAsync(cancellationToken);
        }

        return warned;
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
