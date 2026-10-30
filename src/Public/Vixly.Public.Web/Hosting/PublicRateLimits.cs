using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;
using Vixly.Public.Web.Resources;
using Microsoft.Extensions.DependencyInjection;

namespace Vixly.Public.Web.Hosting;

// =========================================================
// FILE: PublicRateLimits.cs
//
// PURPOSE:
// Limits on the public site's own form posts, per visitor IP (Stage 2, S-6).
//
// RESPONSIBILITIES:
// - Name and add the form-post policies
//
// IMPORTANT NOTES:
// - Only POST requests count; reading a page is never limited here
// - Platform.Api applies its own limits per visitor IP as well
// =========================================================
public static class PublicRateLimits
{
    public const string Contact = "public-contact";

    public const string Start = "public-start";

    public const string Registration = "public-registration";

    public const string Pay = "public-pay";

    public static IServiceCollection AddPublicRateLimits(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = (context, cancellationToken) =>
            {
                // A body keeps the status-code page (the not-found page) from answering instead.
                IStringLocalizer<PublicText> text = context.HttpContext.RequestServices.GetRequiredService<IStringLocalizer<PublicText>>();
                context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
                return new ValueTask(context.HttpContext.Response.WriteAsync(text["public.too_many"], cancellationToken));
            };
            Add(options, Contact, 5);
            Add(options, Start, 5);
            Add(options, Registration, 30);
            Add(options, Pay, 20);
        });

        return services;
    }

    private static void Add(RateLimiterOptions options, string name, int perHour)
    {
        options.AddPolicy(name, http => HttpMethods.IsPost(http.Request.Method)
            ? RateLimitPartition.GetFixedWindowLimiter(
                http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = perHour, Window = TimeSpan.FromHours(1), QueueLimit = 0 })
            : RateLimitPartition.GetNoLimiter("read"));
    }
}
