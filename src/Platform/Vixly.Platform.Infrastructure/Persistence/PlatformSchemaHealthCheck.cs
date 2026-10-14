using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Vixly.Platform.Infrastructure.Persistence;

// =========================================================
// FILE: PlatformSchemaHealthCheck.cs
//
// PURPOSE:
// Readiness check: the platform schema is at the migration
// level this build expects (D-1).
//
// RESPONSIBILITIES:
// - Report Unhealthy when migrations are pending, so the
//   host is taken out of rotation until the database script
//   has been run
//
// IMPORTANT NOTES:
// - Never migrates; only reads the history table
// - The response body carries the status word only
// =========================================================
internal sealed class PlatformSchemaHealthCheck : IHealthCheck
{
    private readonly PlatformDbContext _context;

    public PlatformSchemaHealthCheck(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<string> pending = await _context.Database.GetPendingMigrationsAsync(cancellationToken);

        return pending.Any()
            ? HealthCheckResult.Unhealthy("Pending migrations.")
            : HealthCheckResult.Healthy();
    }
}
