using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace Vixly.Platform.Api.Hosting;

// =========================================================
// FILE: PlatformApiRateLimits.cs
//
// PURPOSE:
// Named rate-limit policies of the public registration API (S-6).
//
// RESPONSIBILITIES:
// - Register one fixed-window policy per endpoint group, per client
//   IP
//
// IMPORTANT NOTES:
// - The client IP is CF-Connecting-IP, trusted only from
//   Cloudflare (Shared.Hosting forwarded headers)
// - Per-registration limits (3 links an hour) are in the domain
// - Values are the Slice 2 design note, section 6
// =========================================================
public static class PlatformApiRateLimits
{
    public const string SlugCheck = "registration-slug-check";

    public const string Start = "registration-start";

    public const string Verify = "registration-verify";

    public const string Change = "registration-change";

    public const string Status = "registration-status";

    public static IServiceCollection AddPlatformApiRateLimits(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            AddPerIp(options, SlugCheck, 30, TimeSpan.FromMinutes(1));
            AddPerIp(options, Start, 5, TimeSpan.FromHours(1));
            AddPerIp(options, Verify, 10, TimeSpan.FromHours(1));
            AddPerIp(options, Change, 10, TimeSpan.FromHours(1));
            AddPerIp(options, Status, 60, TimeSpan.FromHours(1));
        });

        return services;
    }

    private static void AddPerIp(RateLimiterOptions options, string name, int permits, TimeSpan window)
    {
        options.AddPolicy(name, context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permits,
                Window = window,
                QueueLimit = 0
            }));
    }
}
