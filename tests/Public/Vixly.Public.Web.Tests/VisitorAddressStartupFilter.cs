using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Vixly.Public.Web.Tests;

// =========================================================
// FILE: VisitorAddressStartupFilter.cs
//
// PURPOSE:
// Gives every test request a visitor address, as a real connection has.
//
// RESPONSIBILITIES:
// - Set the remote address before the site pipeline runs
//
// IMPORTANT NOTES:
// - The test server leaves the remote address empty; limits and the caller token need one
// =========================================================
internal sealed class VisitorAddressStartupFilter : IStartupFilter
{
    public static readonly IPAddress Visitor = IPAddress.Parse("203.0.113.7");

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.Use((http, following) =>
            {
                http.Connection.RemoteIpAddress ??= Visitor;
                return following(http);
            });
            next(app);
        };
    }
}
