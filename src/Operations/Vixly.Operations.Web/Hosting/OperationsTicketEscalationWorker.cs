using Vixly.Operations.Application.Platform;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Tickets;
using Vixly.Operations.Domain.Jobs;

namespace Vixly.Operations.Web.Hosting;

// =========================================================
// FILE: OperationsTicketEscalationWorker.cs
//
// PURPOSE:
// Tells the managers about tickets that wait too long, every 15 minutes (owner, 2026-10-29, ticket desk section 5).
//
// RESPONSIBILITIES:
// - Find the companies with a ticket not resolved for more than an hour, in a system scope
// - For each, in its own scope: its portal address from Platform, then the escalations due
// - Record the run (ADR-0019 job runs)
//
// IMPORTANT NOTES:
// - The emails go through the outbox, one message per ticket and step
// - A failed company is logged and tried again on the next round
// =========================================================
public sealed class OperationsTicketEscalationWorker : BackgroundService
{
    public const string JobName = "ops.tickets.escalations";

    public static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

    private readonly IServiceScopeFactory _scopes;

    private readonly IConfiguration _configuration;

    private readonly TimeProvider _timeProvider;

    private readonly ILogger<OperationsTicketEscalationWorker> _logger;

    // P-5: the jobs of a site start one after another.
    private readonly Vixly.Shared.Hosting.Jobs.VixlyJobStagger _stagger;

    public OperationsTicketEscalationWorker(IServiceScopeFactory scopes, IConfiguration configuration, TimeProvider timeProvider, ILogger<OperationsTicketEscalationWorker> logger, Vixly.Shared.Hosting.Jobs.VixlyJobStagger stagger)
    {
        _stagger = stagger ?? throw new ArgumentNullException(nameof(stagger));
        _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // The portal address of each company (for the links) comes from Platform.
        if (string.IsNullOrWhiteSpace(_configuration["OperationsPlatform:CallerPrivateKey"]))
        {
            _logger.LogWarning("No Platform caller key is set: ticket escalations are not sent.");
            return;
        }

        // P-5 (owner, 2026-11-04): not every job at the same moment as the first visitor after a start-up.
        await _stagger.WaitTurnAsync(stoppingToken);

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
                _logger.LogError(exception, "Ticket escalation round failed; it will run again.");
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
            tenants = await system.ServiceProvider.GetRequiredService<IOperationsTicketRepository>().ListTenantsWithEscalatableAsync(utcNow.AddHours(-1), cancellationToken);
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
                sent += await services.GetRequiredService<OperationsTicketEscalations>().RunAsync(tenantId, account.Slug, utcNow, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                failed++;
                _logger.LogError(exception, "Ticket escalations failed for tenant {TenantId}; they will be tried again.", tenantId);
            }
        }

        await using (AsyncServiceScope record = _scopes.CreateAsyncScope())
        {
            record.ServiceProvider.GetRequiredService<IOperationsTenantScope>().EnterSystem();
            OperationsJobRun run = OperationsJobRun.Start(JobName, utcNow);
            run.Finish(sent, failed, failed > 0 ? "ops.tickets.escalations_failed" : null, _timeProvider.GetUtcNow());
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
