using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace Vixly.Platform.Api.Internal;

// =========================================================
// FILE: PlatformInternalRateLimits.cs
//
// PURPOSE:
// Rate limits of the internal calls, per caller and tenant, not per IP (Slice 9, D-K6).
//
// RESPONSIBILITIES:
// - Name and add the internal limit policies
//
// IMPORTANT NOTES:
// - Device checks: 60 per minute per device (D-L12)
// - Edge portal access is cached 60 seconds, so its limit is high
// =========================================================
public static class PlatformInternalRateLimits
{
    public const string Portal = "internal-portal";

    public const string Tenant = "internal-tenant";

    public const string Device = "internal-device";

    public const string Jobs = "internal-jobs";

    public const string Events = "internal-events";

    public static IServiceCollection AddPlatformInternalRateLimits(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddRateLimiter(options =>
        {
            Add(options, Portal, 3000, http => PlatformInternalRequest.PartitionKey(http));
            Add(options, Tenant, 600, http => PlatformInternalRequest.PartitionKey(http));
            Add(options, Device, 60, http => PlatformInternalRequest.PartitionKey(http, ":" + http.Request.RouteValues["deviceId"]));
            Add(options, Jobs, 120, http => PlatformInternalRequest.PartitionKey(http));
            Add(options, Events, 240, http => PlatformInternalRequest.PartitionKey(http));
        });

        return services;
    }

    private static void Add(RateLimiterOptions options, string name, int perMinute, Func<HttpContext, string> partition)
    {
        options.AddPolicy(name, http => RateLimitPartition.GetFixedWindowLimiter(
            partition(http),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = perMinute,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    }
}
