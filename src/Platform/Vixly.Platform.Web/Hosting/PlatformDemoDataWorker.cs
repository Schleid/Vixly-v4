using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Vixly.Platform.Web.Hosting;

// =========================================================
// FILE: PlatformDemoDataWorker.cs
//
// PURPOSE:
// In Development only, fills the database with demo data when
// the console starts, so new slices show data without running
// the --seed-demo-tenants command again.
//
// RESPONSIBILITIES:
// - Run PlatformDemoTenants, PlatformDemoSubscriptions, then
//   PlatformDemoSales, once at start-up, each in a fresh scope
// - Log what was added, or why nothing was
//
// IMPORTANT NOTES:
// - Registered only in the Development environment; switched off
//   with PlatformDemoData:SeedOnStartup = false
// - Both seeds add only what is missing, so restarts are safe
// - Never stops the console: failures are logged
// =========================================================
public sealed class PlatformDemoDataWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;

    private readonly PlatformDemoDataOptions _options;

    private readonly ILogger<PlatformDemoDataWorker> _logger;

    public PlatformDemoDataWorker(IServiceScopeFactory scopes, IOptions<PlatformDemoDataOptions> options, ILogger<PlatformDemoDataWorker> logger)
    {
        _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.SeedOnStartup)
        {
            return;
        }

        try
        {
            await using AsyncServiceScope scope = _scopes.CreateAsyncScope();
            string tenants = await ActivatorUtilities.CreateInstance<PlatformDemoTenants>(scope.ServiceProvider).SeedAsync(stoppingToken);
            _logger.LogInformation("Demo data: {Result}", tenants);

            await using AsyncServiceScope second = _scopes.CreateAsyncScope();
            string subscriptions = await ActivatorUtilities.CreateInstance<PlatformDemoSubscriptions>(second.ServiceProvider).SeedAsync(stoppingToken);
            _logger.LogInformation("Demo data: {Result}", subscriptions);

            await using AsyncServiceScope third = _scopes.CreateAsyncScope();
            string sales = await ActivatorUtilities.CreateInstance<PlatformDemoSales>(third.ServiceProvider).SeedAsync(stoppingToken);
            _logger.LogInformation("Demo data: {Result}", sales);

            await using AsyncServiceScope fourth = _scopes.CreateAsyncScope();
            string billing = await ActivatorUtilities.CreateInstance<PlatformDemoBilling>(fourth.ServiceProvider).SeedAsync(stoppingToken);
            _logger.LogInformation("Demo data: {Result}", billing);

            await using AsyncServiceScope fifth = _scopes.CreateAsyncScope();
            string licensing = await ActivatorUtilities.CreateInstance<PlatformDemoLicensing>(fifth.ServiceProvider).SeedAsync(stoppingToken);
            _logger.LogInformation("Demo data: {Result}", licensing);

            await using AsyncServiceScope sixth = _scopes.CreateAsyncScope();
            string legal = await ActivatorUtilities.CreateInstance<PlatformDemoLegal>(sixth.ServiceProvider).SeedAsync(stoppingToken);
            _logger.LogInformation("Demo data: {Result}", legal);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (SqlException exception) when (exception.Number == 208)
        {
            _logger.LogWarning("Demo data skipped: a table is missing. Run build/scripts/sql/vixly-v4-database.sql on this database first.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Demo data could not be added.");
        }
    }
}
