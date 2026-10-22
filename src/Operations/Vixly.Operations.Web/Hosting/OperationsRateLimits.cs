using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Vixly.Operations.Web.Tenancy;

namespace Vixly.Operations.Web.Hosting;

// =========================================================
// FILE: OperationsRateLimits.cs
//
// PURPOSE:
// Rate limits of the anonymous staff pages (Slice 0 section 6, Slice 1 section 6, S-6).
//
// RESPONSIBILITIES:
// - Status pages: 60 per minute per address (global)
// - Sign-in: 10 per minute; second factor: 20 per minute; invitation: 10 per hour; public tracking: 30 per minute, per address
// - The counter's shipment estimate (Slice 5D): 240 per minute per address (a front desk's clerks typing behind one address)
//
// IMPORTANT NOTES:
// - Runs after the tenant check, so a rewritten path counts as a status page
// - Over the limit: 429 with no body
// - Per-account lockout and per-sign-in attempts live in the domain
// =========================================================
public static class OperationsRateLimits
{
    public const string SignIn = "sign-in";

    public const string SecondFactor = "second-factor";

    public const string AccountLink = "account-link";

    public const string Tracking = "tracking";

    public const string PasswordReset = "password-reset";

    public const string Estimate = "estimate";

    // Slice 5F step 4: Stripe's events to a company's webhook, per host (Stripe sends from many addresses).
    public const string CardWebhook = "card-webhook";

    // Slice 5F step 3: codes typed on a customer screen, per address.
    public const string ScreenPair = "screen-pair";

    public const int StatusPagesPerMinute = 60;

    private static readonly string[] StatusPages =
    {
        OperationsTenantMiddleware.NotFoundPath,
        OperationsTenantMiddleware.NotAvailablePath,
        OperationsTenantMiddleware.UnavailablePath
    };

    public static IServiceCollection AddOperationsRateLimits(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(http =>
                StatusPages.Any(page => http.Request.Path.Equals(page, StringComparison.OrdinalIgnoreCase))
                    ? RateLimitPartition.GetFixedWindowLimiter(Address(http), _ => Window(StatusPagesPerMinute, TimeSpan.FromMinutes(1)))
                    : RateLimitPartition.GetNoLimiter(string.Empty));
            options.AddPolicy(SignIn, http => RateLimitPartition.GetFixedWindowLimiter(Address(http), _ => Window(10, TimeSpan.FromMinutes(1))));
            options.AddPolicy(SecondFactor, http => RateLimitPartition.GetFixedWindowLimiter(Address(http), _ => Window(20, TimeSpan.FromMinutes(1))));
            options.AddPolicy(Tracking, http => RateLimitPartition.GetFixedWindowLimiter(Address(http), _ => Window(30, TimeSpan.FromMinutes(1))));
            options.AddPolicy(PasswordReset, http => RateLimitPartition.GetFixedWindowLimiter(Address(http), _ => Window(5, TimeSpan.FromHours(1))));
            options.AddPolicy(AccountLink, http => RateLimitPartition.GetFixedWindowLimiter(Address(http), _ => Window(10, TimeSpan.FromHours(1))));
            options.AddPolicy(Estimate, http => RateLimitPartition.GetFixedWindowLimiter(Address(http), _ => Window(240, TimeSpan.FromMinutes(1))));
            options.AddPolicy(ScreenPair, http => RateLimitPartition.GetFixedWindowLimiter(Address(http), _ => Window(10, TimeSpan.FromMinutes(10))));
            options.AddPolicy(CardWebhook, http => RateLimitPartition.GetFixedWindowLimiter(http.Request.Host.Host.ToLowerInvariant(), _ => Window(600, TimeSpan.FromMinutes(1))));
        });
    }

    private static string Address(HttpContext http) => http.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static FixedWindowRateLimiterOptions Window(int permits, TimeSpan window) => new() { PermitLimit = permits, Window = window, QueueLimit = 0 };
}
