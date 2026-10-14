using Vixly.Platform.Application.Security;

namespace Vixly.Platform.Web.Hosting;

// =========================================================
// FILE: PlatformSystemRoleStartup.cs
//
// PURPOSE:
// Brings the system roles in line with the code when the
// console starts (Slice 1: "permissions set from code at deploy").
//
// RESPONSIBILITIES:
// - Run PlatformSystemRoleSynchronizer once, in its own scope
// - Log what changed, and any name conflict to fix by hand
//
// IMPORTANT NOTES:
// - A failure is logged, never fatal: the console still starts,
//   for example before the database script has run
// - Safe on several instances: the unique SystemKey index stops
//   a second copy, and the next start corrects any drift
// =========================================================
public sealed class PlatformSystemRoleStartup : IHostedService
{
    private readonly IServiceScopeFactory _scopes;

    private readonly ILogger<PlatformSystemRoleStartup> _logger;

    public PlatformSystemRoleStartup(IServiceScopeFactory scopes, ILogger<PlatformSystemRoleStartup> logger)
    {
        _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using AsyncServiceScope scope = _scopes.CreateAsyncScope();
            PlatformSystemRoleSyncResult result = await scope.ServiceProvider
                .GetRequiredService<PlatformSystemRoleSynchronizer>()
                .SynchronizeAsync(cancellationToken);

            if (result.Created.Count + result.Corrected.Count > 0)
            {
                _logger.LogInformation(
                    "System roles synchronised. Created: {Created}. Corrected: {Corrected}. Saved: {Saved}.",
                    string.Join(", ", result.Created),
                    string.Join(", ", result.Corrected),
                    result.Saved);
            }

            if (result.Conflicts.Count > 0)
            {
                _logger.LogWarning(
                    "System roles not created because a custom role already uses the name: {Conflicts}. Rename that role.",
                    string.Join(", ", result.Conflicts));
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "System roles could not be synchronised. The console starts without them.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
