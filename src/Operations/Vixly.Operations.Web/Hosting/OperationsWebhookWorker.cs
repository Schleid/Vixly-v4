using Vixly.Operations.Application.Api;
using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Web.Hosting;

// =========================================================
// FILE: OperationsWebhookWorker.cs
//
// PURPOSE:
// Sends the companies’ webhooks every 10 seconds (Slice 10).
//
// RESPONSIBILITIES:
// - List the companies with an active address (system scope); for each, in its own scope, queue new events and send what is due
// - Up to four companies at a time, each within a 30-second budget; events older than a week pruned once an hour
//
// IMPORTANT NOTES:
// - One company’s failure never stops the others; several hosts may run it (deliveries are leased)
// =========================================================
public sealed class OperationsWebhookWorker : BackgroundService
{
    public static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);

    public static readonly TimeSpan Budget = TimeSpan.FromSeconds(30);

    private static DateTimeOffset _prunedUtc = DateTimeOffset.MinValue;

    private readonly IServiceScopeFactory _scopes;

    private readonly ILogger<OperationsWebhookWorker> _logger;

    public OperationsWebhookWorker(IServiceScopeFactory scopes, ILogger<OperationsWebhookWorker> logger)
    {
        _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // One round over every company (also called by tests).
    public static async Task RunOnceAsync(IServiceScopeFactory scopes, ILogger logger, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scopes);
        ArgumentNullException.ThrowIfNull(logger);
        IReadOnlyList<Guid> tenants;

        await using (AsyncServiceScope system = scopes.CreateAsyncScope())
        {
            system.ServiceProvider.GetRequiredService<IOperationsTenantScope>().EnterSystem();
            IOperationsApiRepository store = system.ServiceProvider.GetRequiredService<IOperationsApiRepository>();
            tenants = await store.TenantsWithActiveEndpointsAsync(cancellationToken);

            if (DateTimeOffset.UtcNow - _prunedUtc > TimeSpan.FromHours(1))
            {
                await store.PruneEventsAsync(DateTimeOffset.UtcNow - Vixly.Operations.Domain.Api.OperationsApiEvent.Keep, cancellationToken);
                _prunedUtc = DateTimeOffset.UtcNow;
            }
        }

        // A few companies at a time, each in its own scope and budget: one slow receiver never holds up the others.
        await Parallel.ForEachAsync(tenants, new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = cancellationToken }, async (tenantId, token) =>
        {
            try
            {
                await using AsyncServiceScope scope = scopes.CreateAsyncScope();
                scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenantId);
                await scope.ServiceProvider.GetRequiredService<OperationsWebhookDesk>().RunAsync(tenantId, Budget, token);
            }
            catch (Exception exception) when (exception is not OperationCanceledException || !token.IsCancellationRequested)
            {
                logger.LogError(exception, "Webhooks failed for tenant {TenantId}; they run again next round.", tenantId);
            }
        });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(Interval);

        do
        {
            try
            {
                await RunOnceAsync(_scopes, _logger, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Webhook round failed; it will run again.");
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
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return false;
        }
    }
}
