using Microsoft.Extensions.Options;
using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Platform;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Setup;
using Vixly.Operations.Domain.Messaging;

namespace Vixly.Operations.Web.Hosting;

// =========================================================
// FILE: OperationsDemoDataWorker.cs
//
// PURPOSE:
// In Development only, sets up the Owner of each active Platform demo tenant, as the registration event would for a real tenant.
//
// RESPONSIBILITIES:
// - Ask Platform which demo tenants are active
// - Run the real owner invitation for each tenant that has no staff yet: Owner, brand, invitation email
// - Fill the companies listed in ScenarioSlugs with a sample working day (OperationsDemoScenario)
// - Log what was done, or why not
//
// IMPORTANT NOTES:
// - Registered only in the Development environment; switched off with OperationsDemoData:SeedOnStartup = false
// - Uses the same handler as the account_setup_ready event, so the demo follows the real path; emails go to the .mail folder
// - Adds only what is missing, so restarts are safe; waits for Platform when it is not up yet; never stops the host
// - On a new database, Platform adds its demo tenants a little later (after the first SuperUser): a demo company not found yet
//   is asked again every 30 seconds for up to WaitForTenantsMinutes (an hour), so no restart is needed (owner report, 2026-10-20)
// =========================================================
public sealed class OperationsDemoDataWorker : BackgroundService
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(15);

    private const int Attempts = 8;

    private static readonly TimeSpan NotYetDelay = TimeSpan.FromSeconds(30);


    private readonly IServiceScopeFactory _scopes;

    private readonly OperationsDemoDataOptions _options;

    private readonly ILogger<OperationsDemoDataWorker> _logger;

    public OperationsDemoDataWorker(IServiceScopeFactory scopes, IOptions<OperationsDemoDataOptions> options, ILogger<OperationsDemoDataWorker> logger)
    {
        _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.SeedOnStartup || _options.Slugs.Count == 0)
        {
            return;
        }

        int invited = 0;
        int ready = 0;
        int skipped = 0;
        List<string> waiting = _options.Slugs.ToList();

        for (int round = 0; waiting.Count > 0; round++)
        {
            if (round > 0)
            {
                if (round == 1)
                {
                    _logger.LogInformation("Demo data: waiting for Platform's demo tenants ({Slugs}); they are set up as soon as Platform has them.", string.Join(", ", waiting));
                }

                await Task.Delay(NotYetDelay, stoppingToken);
            }

            List<string> notYet = new();

            foreach (string slug in waiting)
            {
                try
                {
                    switch (await SeedTenantAsync(slug, stoppingToken))
                    {
                        case Outcome.Invited:
                            invited++;
                            break;
                        case Outcome.AlreadySetUp:
                            ready++;
                            break;
                        case Outcome.NotYet when round < _options.WaitForTenantsMinutes * 2:
                            notYet.Add(slug);
                            break;
                        default:
                            skipped++;
                            break;
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (OperationsPlatformUnavailableException)
                {
                    _logger.LogWarning("Demo data skipped: Platform.Api did not answer. Start it and restart Operations.Web.");
                    return;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Demo data: the owner of {Slug} could not be set up.", slug);
                    skipped++;
                }
            }

            waiting = notYet;
        }

        _logger.LogInformation(
            "Demo data: {Invited} owners invited (emails in the .mail folder), {Ready} tenants already set up, {Skipped} not active or not found.",
            invited, ready, skipped);
    }

    private async Task<Outcome> SeedTenantAsync(string slug, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = _scopes.CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;
        OperationsPortalAnswer portal = await PortalAsync(services.GetRequiredService<IOperationsPlatformClient>(), slug, cancellationToken);

        // Not in Platform yet (a new database): asked again later. Found but not active: left alone.
        if (!portal.Found || portal.TenantId is not { } tenantId)
        {
            return Outcome.NotYet;
        }

        if (!string.Equals(portal.Status, "active", StringComparison.Ordinal))
        {
            return Outcome.Skipped;
        }

        services.GetRequiredService<IOperationsTenantScope>().Enter(tenantId);

        bool existed = await services.GetRequiredService<IOperationsUserRepository>().AnyAsync(cancellationToken);

        if (!existed)
        {
            await InviteOwnerAsync(services, tenantId, cancellationToken);
            _logger.LogInformation("Demo data: owner of {Slug} invited.", slug);
        }

        if (_options.SeedStaff)
        {
            await using AsyncServiceScope staffScope = _scopes.CreateAsyncScope();
            staffScope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenantId);
            int ready = await staffScope.ServiceProvider.GetRequiredService<OperationsDemoStaffSeeder>().SeedAsync(tenantId, slug, cancellationToken);

            if (ready > 0)
            {
                _logger.LogInformation("Demo data: {Count} staff of {Slug} can sign in (see docs/operations/DEVELOPMENT-DEMO-USERS.md).", ready, slug);
            }

            // A working day of sample data for the companies listed (owner, 2026-10-19); the first is the main story, the others check isolation.
            int place = _options.ScenarioSlugs.ToList().IndexOf(slug);

            try
            {
                if (place >= 0 && await scope.ServiceProvider.GetRequiredService<OperationsDemoScenario>().SeedAsync(tenantId, slug, place == 0, cancellationToken))
                {
                    _logger.LogInformation("Demo data: sample day added to {Slug} (see docs/operations/DEVELOPMENT-DEMO-DATA.md).", slug);
                }
                else if (place >= 0 && await scope.ServiceProvider.GetRequiredService<OperationsDemoScenario>().TopUpAsync(tenantId, slug, place == 0, cancellationToken))
                {
                    _logger.LogInformation("Demo data: shipping supplies added to {Slug}.", slug);
                }
            }
            catch (InvalidOperationException exception)
            {
                // A rule refused a step (for example a code already used by data entered by hand): the rest of the start-up goes on.
                _logger.LogWarning("Demo data: the sample day of {Slug} stopped. {Reason}", slug, exception.Message);
            }
        }

        return existed ? Outcome.AlreadySetUp : Outcome.Invited;
    }

    private static async Task InviteOwnerAsync(IServiceProvider services, Guid tenantId, CancellationToken cancellationToken)
    {
        IOperationsOutboxHandler handler = services.GetServices<IOperationsOutboxHandler>()
            .Single(item => item.MessageType == OperationsSetupMessages.OwnerInvitationRequested);
        await handler.HandleAsync(
            OperationsOutboxMessage.Create(OperationsSetupMessages.OwnerInvitationRequested, tenantId, tenantId, "{}", DateTimeOffset.UtcNow),
            cancellationToken);
    }

    // Platform.Api may still be starting when both hosts are launched together.
    private static async Task<OperationsPortalAnswer> PortalAsync(IOperationsPlatformClient platform, string slug, CancellationToken cancellationToken)
    {
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                return await platform.GetPortalAsync(slug, cancellationToken);
            }
            catch (OperationsPlatformUnavailableException) when (attempt < Attempts)
            {
                await Task.Delay(RetryDelay, cancellationToken);
            }
        }
    }

    private enum Outcome
    {
        Skipped,
        NotYet,
        Invited,
        AlreadySetUp
    }
}
