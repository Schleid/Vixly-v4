using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Vixly.Public.Web.Platform;

// =========================================================
// FILE: PlatformApiHealthCheck.cs
//
// PURPOSE:
// Ready only while Platform.Api answers (S-12).
//
// RESPONSIBILITIES:
// - Call Platform.Api health
//
// IMPORTANT NOTES:
// - Live never depends on Platform
// =========================================================
public sealed class PlatformApiHealthCheck : IHealthCheck
{
    private readonly PlatformApi _api;

    public PlatformApiHealthCheck(PlatformApi api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return await _api.AliveAsync(cancellationToken) ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("Platform.Api does not answer.");
    }
}
