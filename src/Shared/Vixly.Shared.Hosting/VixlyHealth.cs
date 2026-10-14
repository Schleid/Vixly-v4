using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Vixly.Shared.Hosting;

// =========================================================
// FILE: VixlyHealth.cs
//
// PURPOSE:
// The two health endpoints every V4 host exposes (S-12).
//
// RESPONSIBILITIES:
// - /health/live: the process is running (no checks)
// - /health/ready: every check tagged "ready" passes
//   (database reachable, schema at the build's migration)
//
// IMPORTANT NOTES:
// - Anonymous, no detail in the body: only the status word
// - Hosts tag their readiness checks with ReadyTag
// =========================================================
public static class VixlyHealth
{
    public const string ReadyTag = "ready";

    public static IEndpointRouteBuilder MapVixlyHealth(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(ReadyTag)
        });

        return endpoints;
    }
}
