using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Vixly.Customer.Web.Portal;

// =========================================================
// FILE: CustomerRateLimits.cs
//
// PURPOSE:
// Rate limits of the anonymous customer forms (S-6, design section 6).
//
// RESPONSIBILITIES:
// - Sign-in, sign-up, emailed links and link pages, per address
// - The shipment estimate beside the shipment form (Slice 5D): 60 per minute per address
//
// IMPORTANT NOTES:
// - The status-page limit stays the global limiter
// =========================================================
public static class CustomerRateLimits
{
    public const string SignIn = "customer-sign-in";

    public const string SignUp = "customer-sign-up";

    public const string Email = "customer-email";

    public const string Link = "customer-link";

    public const string Estimate = "customer-estimate";

    public static RateLimiterOptions AddCustomerPortalLimits(this RateLimiterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        Add(options, SignIn, 10, TimeSpan.FromMinutes(1));
        Add(options, SignUp, 5, TimeSpan.FromHours(1));
        Add(options, Email, 5, TimeSpan.FromHours(1));
        Add(options, Link, 10, TimeSpan.FromHours(1));
        Add(options, Estimate, 60, TimeSpan.FromMinutes(1));
        return options;
    }

    private static void Add(RateLimiterOptions options, string name, int permits, TimeSpan window)
    {
        options.AddPolicy(name, http => RateLimitPartition.GetFixedWindowLimiter(
            http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = permits, Window = window, QueueLimit = 0 }));
    }
}
