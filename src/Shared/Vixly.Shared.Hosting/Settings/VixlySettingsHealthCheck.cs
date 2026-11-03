using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Vixly.Shared.Hosting.Settings;

// =========================================================
// FILE: VixlySettingsHealthCheck.cs
//
// PURPOSE:
// Reports the settings check on /health/ready (settings check, 2026-10-23).
//
// RESPONSIBILITIES:
// - Healthy when nothing is missing; degraded when a setting the site can run without is missing
//
// IMPORTANT NOTES:
// - Names the settings, never their values
// - The description is shown only on the detailed health page (VixlyHealthDetail)
// =========================================================
public sealed class VixlySettingsHealthCheck : IHealthCheck
{
    private readonly VixlySettingsReport _report;

    public VixlySettingsHealthCheck(VixlySettingsReport report)
    {
        _report = report ?? throw new ArgumentNullException(nameof(report));
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        string waived = _report.Waived.Count == 0 ? string.Empty : "; waived on purpose: " + string.Join(", ", _report.Waived);
        return Task.FromResult(_report.Problems.Count == 0
            ? HealthCheckResult.Healthy("settings: OK" + waived)
            : HealthCheckResult.Degraded("settings: " + string.Join("; ", _report.Problems.Select(problem => problem.ToString())) + waived));
    }
}
