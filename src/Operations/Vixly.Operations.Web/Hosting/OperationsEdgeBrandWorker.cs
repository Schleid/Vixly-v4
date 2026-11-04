using Vixly.Operations.Application.Branding;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Jobs;

namespace Vixly.Operations.Web.Hosting;

// =========================================================
// FILE: OperationsEdgeBrandWorker.cs
//
// PURPOSE:
// Every night, sends each company's brand to the Cloudflare Worker again (Worker design, section 3; owner 2026-10-28).
//
// RESPONSIBILITIES:
// - Once a day, in the round that falls in the 06:00 UTC hour: every company with a brand, in its own tenant scope
// - Record the round (ops.edge.brands): how many were taken, how many failed
//
// IMPORTANT NOTES:
// - Without Edge:BrandUrl the worker does not run (staging and development have no Worker)
// - Publishing the website sends at once through the outbox; this round repairs anything missed
// - One company failing never stops the others
// =========================================================
public sealed class OperationsEdgeBrandWorker : BackgroundService
{
    public const string JobName = "ops.edge.brands";

    public const int SendHourUtc = 6;

    public static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopes;

    private readonly IConfiguration _configuration;

    private readonly TimeProvider _timeProvider;

    private readonly ILogger<OperationsEdgeBrandWorker> _logger;

    // P-5: the jobs of a site start one after another.
    private readonly Vixly.Shared.Hosting.Jobs.VixlyJobStagger _stagger;

    public OperationsEdgeBrandWorker(IServiceScopeFactory scopes, IConfiguration configuration, TimeProvider timeProvider, ILogger<OperationsEdgeBrandWorker> logger, Vixly.Shared.Hosting.Jobs.VixlyJobStagger stagger)
    {
        _stagger = stagger ?? throw new ArgumentNullException(nameof(stagger));
        _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_configuration["Edge:BrandUrl"]))
        {
            return;
        }

        // P-5 (owner, 2026-11-04): not every job at the same moment as the first visitor after a start-up.
        await _stagger.WaitTurnAsync(stoppingToken);

        using PeriodicTimer timer = new(Interval);

        do
        {
            try
            {
                if (_timeProvider.GetUtcNow().Hour == SendHourUtc)
                {
                    await RunAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "The nightly brand round failed; it will run again tomorrow.");
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
            tenants = await system.ServiceProvider.GetRequiredService<IOperationsBrandRepository>().ListTenantIdsAsync(cancellationToken);
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

                if (await services.GetRequiredService<OperationsEdgeBrands>().SendAsync(account, cancellationToken) == OperationsEdgeSendResult.Sent)
                {
                    sent++;
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                failed++;
                _logger.LogError(exception, "The brand of tenant {TenantId} could not be sent to the Cloudflare Worker.", tenantId);
            }
        }

        await using (AsyncServiceScope record = _scopes.CreateAsyncScope())
        {
            record.ServiceProvider.GetRequiredService<IOperationsTenantScope>().EnterSystem();
            OperationsJobRun run = OperationsJobRun.Start(JobName, utcNow);
            run.Finish(sent, failed, failed > 0 ? "ops.edge.brands_failed" : null, _timeProvider.GetUtcNow());
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
