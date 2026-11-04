using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Vixly.Shared.Hosting.Diagnostics;

// =========================================================
// FILE: VixlyRequestTiming.cs
//
// PURPOSE:
// Measures where each request spends its time (performance plan P-1, owner 2026-11-04).
//
// RESPONSIBILITIES:
// - Count database commands and outgoing calls per request
// - Log requests slower than VixlyTiming:SlowRequestMs, with their route and the split
// - Add a Server-Timing header only when the setting is on and the site allows it for this person
//
// IMPORTANT NOTES:
// - The log names the route pattern, never the address or its query (no ids, tokens or emails)
// - The header is off by default; each site decides who may see it (staff owners, TechCrafters staff), never the public site or the customer portal (plan section 5)
// =========================================================
public static class VixlyRequestTiming
{
    public const string LoggerName = "Vixly.Timing";

    public static IServiceCollection AddVixlyRequestTiming(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<VixlyRequestTimingOptions>().Bind(configuration.GetSection(VixlyRequestTimingOptions.SectionName));
        services.AddHttpContextAccessor();
        services.AddSingleton<VixlySqlTimingObserver>();
        services.AddTransient<VixlyCallTimingHandler>();
        services.ConfigureHttpClientDefaults(client => client.AddHttpMessageHandler<VixlyCallTimingHandler>());
        return services;
    }

    // mayShowHeader: who may see the Server-Timing header on this site; null shows it to no one.
    public static IApplicationBuilder UseVixlyRequestTiming(this IApplicationBuilder app, Func<HttpContext, Task<bool>>? mayShowHeader = null)
    {
        ArgumentNullException.ThrowIfNull(app);

        VixlySqlTimingObserver observer = app.ApplicationServices.GetRequiredService<VixlySqlTimingObserver>();
        observer.Start();
        ILogger logger = app.ApplicationServices.GetRequiredService<ILoggerFactory>().CreateLogger(LoggerName);
        IOptionsMonitor<VixlyRequestTimingOptions> options = app.ApplicationServices.GetRequiredService<IOptionsMonitor<VixlyRequestTimingOptions>>();

        return app.Use(async (http, next) =>
        {
            VixlyRequestTimes times = new(observer);
            http.Features.Set(times);

            if (options.CurrentValue.ShowHeader && mayShowHeader is not null)
            {
                http.Response.OnStarting(async () =>
                {
                    if (await mayShowHeader(http))
                    {
                        http.Response.Headers["Server-Timing"] = times.Header();
                    }
                });
            }

            try
            {
                await next(http);
            }
            finally
            {
                int slow = options.CurrentValue.SlowRequestMs;

                if (slow > 0 && times.TotalMs >= slow)
                {
                    string route = (http.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "(no route)";
                    logger.LogWarning("Slow request {Method} {Route} {Status} in {TotalMs:F0} ms: {SqlCount} SQL commands in {SqlMs:F0} ms, {CallCount} calls in {CallMs:F0} ms",
                        http.Request.Method, route, http.Response.StatusCode, times.TotalMs, times.SqlCount, times.SqlMs, times.CallCount, times.CallMs);
                }
            }
        });
    }
}
