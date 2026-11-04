using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Vixly.Operations.Infrastructure.Persistence;

// =========================================================
// FILE: OperationsSchemaHealthCheck.cs
//
// PURPOSE:
// Ready only when the ops schema has every migration of this build (S-12).
//
// RESPONSIBILITIES:
// - Report pending migrations as unhealthy
//
// IMPORTANT NOTES:
// - Tagged ready; /health/live never touches the database
// =========================================================
internal sealed class OperationsSchemaHealthCheck : IHealthCheck
{
    private readonly OperationsDbContext _context;

    public OperationsSchemaHealthCheck(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        IEnumerable<string> pending = await _context.Database.GetPendingMigrationsAsync(cancellationToken);
        return pending.Any() ? HealthCheckResult.Unhealthy("Pending migrations.") : HealthCheckResult.Healthy();
    }
}
