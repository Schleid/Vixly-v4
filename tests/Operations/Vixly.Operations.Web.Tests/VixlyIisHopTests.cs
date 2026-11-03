using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Vixly.Shared.Hosting;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: VixlyIisHopTests.cs
//
// PURPOSE:
// Out of process, IIS forwards each request from loopback; the
// app must still see https, the visitor's address and the
// Cloudflare country (production 2026-10-24: Operations Web and
// the customer portal share one pool, so both run out of process).
//
// RESPONSIBILITIES:
// - A small host with the shared pipeline start (country, then
//   forwarded headers) that echoes address, scheme and country
//
// IMPORTANT NOTES:
// - The test stands in for IIS: loopback peer, plain http, and
//   the X-Forwarded-For and X-Forwarded-Proto values IIS adds
// =========================================================
public sealed class VixlyIisHopTests
{
    [Fact]
    public async Task ThroughCloudflareAndIis_TheVisitorHttpsAndCountryAreKept()
    {
        await using WebApplication app = await StartAsync(iis: true);

        string seen = await AskAsync(app, new()
        {
            ["X-Forwarded-For"] = "198.51.100.7, 173.245.48.10:51234",
            ["X-Forwarded-Proto"] = "https",
            ["CF-Connecting-IP"] = "198.51.100.7",
            ["CF-IPCountry"] = "HT"
        });

        Assert.Equal("198.51.100.7|https|HT", seen);
    }

    [Fact]
    public async Task StraightToIis_TheCloudflareHeadersOfAVisitorAreIgnored()
    {
        await using WebApplication app = await StartAsync(iis: true);

        string seen = await AskAsync(app, new()
        {
            ["X-Forwarded-For"] = "203.0.113.9:50000",
            ["X-Forwarded-Proto"] = "https",
            ["CF-Connecting-IP"] = "192.0.2.1",
            ["CF-IPCountry"] = "US"
        });

        Assert.Equal("203.0.113.9|https|", seen);
    }

    [Fact]
    public async Task AVisitorsOwnForwardedFor_CannotPassForCloudflare()
    {
        await using WebApplication app = await StartAsync(iis: true);

        // The visitor claims a Cloudflare address; IIS appends the real one, which is the only value taken.
        string seen = await AskAsync(app, new()
        {
            ["X-Forwarded-For"] = "173.245.48.10, 203.0.113.9:50000",
            ["X-Forwarded-Proto"] = "https",
            ["CF-Connecting-IP"] = "192.0.2.1",
            ["CF-IPCountry"] = "US"
        });

        Assert.Equal("203.0.113.9|https|", seen);
    }

    [Fact]
    public async Task InProcess_NothingChanges()
    {
        await using WebApplication app = await StartAsync(iis: false);

        string seen = await AskAsync(app, new()
        {
            ["X-Forwarded-For"] = "203.0.113.9:50000",
            ["X-Forwarded-Proto"] = "https"
        });

        Assert.Equal("127.0.0.1|http|", seen);
    }

    private static async Task<string> AskAsync(WebApplication app, Dictionary<string, string> headers)
    {
        using HttpClient client = new(app.GetTestServer().CreateHandler()) { BaseAddress = new Uri("http://v4-origin.example/") };
        using HttpRequestMessage request = new(HttpMethod.Get, "/echo");

        foreach ((string name, string value) in headers)
        {
            request.Headers.TryAddWithoutValidation(name, value);
        }

        HttpResponseMessage response = await client.SendAsync(request);

        return await response.Content.ReadAsStringAsync();
    }

    private static async Task<WebApplication> StartAsync(bool iis)
    {
        string[] args =
        {
            "--Hosting:TrustedProxyNetworks:0=173.245.48.0/20",
            "--AllowedHosts=v4-origin.example",
            "--Hosting:IisOutOfProcess=" + (iis ? "true" : "false")
        };

        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder.WebHost.UseTestServer();
        builder.Services.AddVixlyForwardedHeaders(builder.Configuration);
        WebApplication app = builder.Build();

        // IIS out of process: the request reaches the app from loopback, as plain http.
        app.Use((http, next) =>
        {
            http.Connection.RemoteIpAddress = IPAddress.Loopback;
            http.Request.Scheme = "http";
            return next(http);
        });
        app.UseVixlyClientCountry();
        app.UseVixlyForwardedHeaders();
        app.MapGet("/echo", (HttpContext http) => $"{http.Connection.RemoteIpAddress}|{http.Request.Scheme}|{VixlyClientCountry.Of(http)}");
        await app.StartAsync();

        return app;
    }
}
