using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vixly.Platform.Api.Hosting;
using Vixly.Platform.Api.Internal;

namespace Vixly.Platform.Api.Endpoints.Internal;

// =========================================================
// FILE: PlatformInternalEndpoints.cs
//
// PURPOSE:
// Maps the internal calls of Operations and Edge under /internal/v1 (Slice 9, D-K2, D-K9).
//
// RESPONSIBILITIES:
// - Map the internal groups on the internal hosts only
// - Give each group its scope and rate limit
//
// IMPORTANT NOTES:
// - Nothing is mapped while PlatformInternal is not configured
// - The authentication step checks the token before any handler
// =========================================================
public static class PlatformInternalEndpoints
{
    public const string Prefix = "/internal/v1";

    public static IEndpointRouteBuilder MapPlatformInternalEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        PlatformInternalOptions options = endpoints.ServiceProvider.GetRequiredService<IOptions<PlatformInternalOptions>>().Value;

        if (!options.Enabled)
        {
            return endpoints;
        }

        RouteGroupBuilder root = endpoints.MapGroup(Prefix).RequireHost(options.Hosts);

        PlatformInternalPortalEndpoints.Map(Group(root, "/portal", PlatformInternalScopes.Portal, false, PlatformInternalRateLimits.Portal));
        PlatformInternalPortalEndpoints.MapJobs(Group(root, "/jobs", PlatformInternalScopes.Jobs, false, PlatformInternalRateLimits.Jobs));
        PlatformInternalTenantEndpoints.Map(Group(root, "/tenant", PlatformInternalScopes.Tenant, true, PlatformInternalRateLimits.Tenant));
        PlatformInternalDeviceEndpoints.Map(Group(root, "/tenant/devices", PlatformInternalScopes.Tenant, true, PlatformInternalRateLimits.Tenant));
        PlatformInternalDeviceEndpoints.MapChecks(Group(root, "/tenant/devices/{deviceId:guid}", PlatformInternalScopes.Tenant, true, PlatformInternalRateLimits.Device));
        PlatformInternalDeviceEndpoints.MapLinks(Group(root, "/device-links", PlatformInternalScopes.Tenant, false, PlatformInternalRateLimits.Tenant));
        PlatformInternalEventEndpoints.Map(Group(root, "/events", PlatformInternalScopes.Events, false, PlatformInternalRateLimits.Events));

        return endpoints;
    }

    private static RouteGroupBuilder Group(RouteGroupBuilder root, string prefix, string scope, bool tenantRequired, string rateLimit)
    {
        RouteGroupBuilder group = root.MapGroup(prefix);
        group.WithMetadata(new PlatformInternalEndpointMetadata(scope, tenantRequired));
        group.RequireRateLimiting(rateLimit);
        return group;
    }
}
