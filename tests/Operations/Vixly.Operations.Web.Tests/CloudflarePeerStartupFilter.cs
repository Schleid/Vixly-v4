using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: CloudflarePeerStartupFilter.cs
//
// PURPOSE:
// A request that carries CF-IPCountry comes from one of
// Cloudflare's addresses (trusted in appsettings), as it would
// in production.
//
// RESPONSIBILITIES:
// - Give such a request a Cloudflare peer address
//
// IMPORTANT NOTES:
// - Tests only; other requests keep no peer address
// =========================================================
internal sealed class CloudflarePeerStartupFilter : IStartupFilter
{
    private static readonly IPAddress CloudflareEdge = IPAddress.Parse("173.245.48.10");

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.Use((http, following) =>
            {
                if (http.Request.Headers.ContainsKey("CF-IPCountry"))
                {
                    http.Connection.RemoteIpAddress = CloudflareEdge;
                }

                return following(http);
            });
            next(app);
        };
    }
}
