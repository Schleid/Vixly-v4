using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Vixly.Customer.Web.Tenancy;

namespace Vixly.Customer.Web.Hosting;

// =========================================================
// FILE: CustomerStatusPageLimits.cs
//
// PURPOSE:
// Limits how often one address may get the status pages (Slice 0 section 6).
//
// RESPONSIBILITIES:
// - Allow 60 status pages per minute per IP address
// - Leave every other page unlimited
//
// IMPORTANT NOTES:
// - Runs after the tenant check, so a rewritten path counts as a status page
// - Over the limit: 429 with no body
// =========================================================
public static class CustomerStatusPageLimits
{
    public const int PermitsPerMinute = 60;

    private static readonly string[] StatusPages =
    {
        CustomerTenantMiddleware.NotFoundPath,
        CustomerTenantMiddleware.NotAvailablePath,
        CustomerTenantMiddleware.UnavailablePath
    };

    public static IServiceCollection AddCustomerStatusPageLimits(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            Vixly.Customer.Web.Portal.CustomerRateLimits.AddCustomerPortalLimits(options);
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(http =>
                StatusPages.Any(page => http.Request.Path.Equals(page, StringComparison.OrdinalIgnoreCase))
                    ? RateLimitPartition.GetFixedWindowLimiter(
                        http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions { PermitLimit = PermitsPerMinute, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 })
                    : RateLimitPartition.GetNoLimiter(string.Empty));
        });
    }
}
