using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Vixly.Platform.Web.Hosting;

// =========================================================
// FILE: PlatformRateLimits.cs
//
// PURPOSE:
// The named rate-limit policies of the console's anonymous
// endpoints (Global Standard S-6, Slice 1 design section 5).
//
// RESPONSIBILITIES:
// - sign-in: 10 per minute per client IP
// - second-factor: 20 per minute per client IP
// - password-reset: 5 per hour per client IP
// - account-link: 10 per hour per client IP
//
// IMPORTANT NOTES:
// - The client IP is the real one behind Cloudflare
//   (ADR-0011); V3 limited by edge address
// - Per-account lockout and per-sign-in attempt limits live
//   in the domain; these limits stop floods
// =========================================================
public static class PlatformRateLimits
{
    public const string SignIn = "sign-in";

    public const string SecondFactor = "second-factor";

    public const string PasswordReset = "password-reset";

    public const string AccountLink = "account-link";

    public const string Passkey = "passkey";

    public static IServiceCollection AddPlatformRateLimits(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            AddPerIp(options, SignIn, 10, TimeSpan.FromMinutes(1));
            AddPerIp(options, SecondFactor, 20, TimeSpan.FromMinutes(1));
            AddPerIp(options, PasswordReset, 5, TimeSpan.FromHours(1));
            AddPerIp(options, AccountLink, 10, TimeSpan.FromHours(1));
            AddPerIp(options, Passkey, 20, TimeSpan.FromMinutes(1));
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
