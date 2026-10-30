using Vixly.Operations.Application.Billing;
using Vixly.Operations.Application.Platform;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Jobs;

namespace Vixly.Operations.Web.Hosting;

// =========================================================
// FILE: OperationsStatementWorker.cs
//
// PURPOSE:
// Sends the monthly statement notices of credit accounts, checked every hour (Slice 5F step 2b, K-7).
//
// RESPONSIBILITIES:
// - Find the companies with an open credit account, in a system scope
// - For each, in its own scope: its portal address from Platform, then the notices due this month
// - Record the run (ADR-0019 job runs)
//
// IMPORTANT NOTES:
// - Each account is marked once a month, so a notice is sent once even though the worker runs every hour
// - A failed company is logged and tried again next hour
// =========================================================
public sealed class OperationsStatementWorker : BackgroundService
{
    public const string JobName = "ops.credit.statements";

    public static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopes;

    private readonly IConfiguration _configuration;

    private readonly TimeProvider _timeProvider;

    private readonly ILogger<OperationsStatementWorker> _logger;

    public OperationsStatementWorker(IServiceScopeFactory scopes, IConfiguration configuration, TimeProvider timeProvider, ILogger<OperationsStatementWorker> logger)
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
            _logger.LogWarning("No Platform caller key is set: statement notices are not sent.");
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
                _logger.LogError(exception, "Statement notice round failed; it will run again.");
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
            tenants = await system.ServiceProvider.GetRequiredService<IOperationsCreditRepository>().ListTenantsWithAccountsAsync(cancellationToken);
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
                sent += await services.GetRequiredService<OperationsStatementNotices>().SendAsync(tenantId, account.Slug, utcNow, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                failed++;
                _logger.LogError(exception, "Statement notices failed for tenant {TenantId}; they will be tried again.", tenantId);
            }
        }

        await using (AsyncServiceScope record = _scopes.CreateAsyncScope())
        {
            record.ServiceProvider.GetRequiredService<IOperationsTenantScope>().EnterSystem();
            OperationsJobRun run = OperationsJobRun.Start(JobName, utcNow);
            run.Finish(sent, failed, failed > 0 ? "ops.credit.statements_failed" : null, _timeProvider.GetUtcNow());
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
