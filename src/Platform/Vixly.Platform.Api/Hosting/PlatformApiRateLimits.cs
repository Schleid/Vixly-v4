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

    // The sign-up payment page: polled while the first invoice is prepared (two calls per refresh with the status).
    public const string RegistrationInvoice = "registration-invoice";

    public const string Catalogue = "registration-catalogue";

    public const string Webhook = "payments-webhook";

    public const string PaymentLinkView = "payments-link-view";

    public const string PaymentCheckout = "payments-checkout";

    public const string Leads = "public-leads";

    public const string Maintenance = "public-maintenance";

    public const string MaintenancePass = "public-maintenance-pass";

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
            // 2026-10-25: 60 an hour ran out in minutes while the pay page waited for its invoice (status + invoice every refresh).
            AddPerIp(options, Status, 300, TimeSpan.FromHours(1));
            AddPerIp(options, RegistrationInvoice, 300, TimeSpan.FromHours(1));
            AddPerIp(options, Catalogue, 60, TimeSpan.FromMinutes(1));
            AddPerIp(options, Webhook, 600, TimeSpan.FromMinutes(1));
            AddPerIp(options, PaymentLinkView, 60, TimeSpan.FromHours(1));
            AddPerIp(options, PaymentCheckout, 20, TimeSpan.FromHours(1));
            AddPerIp(options, Leads, 5, TimeSpan.FromHours(1));
            // Every site instance asks once a minute; many share an address behind the edge.
            AddPerIp(options, Maintenance, 600, TimeSpan.FromMinutes(1));
            AddPerIp(options, MaintenancePass, 20, TimeSpan.FromMinutes(10));
        });

        return services;
    }

    private static void AddPerIp(RateLimiterOptions options, string name, int permits, TimeSpan window)
    {
        options.AddPolicy(name, context => RateLimitPartition.GetFixedWindowLimiter(
            Vixly.Platform.Api.Internal.PlatformClientAddress.Of(context) ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permits,
                Window = window,
                QueueLimit = 0
            }));
    }
}
