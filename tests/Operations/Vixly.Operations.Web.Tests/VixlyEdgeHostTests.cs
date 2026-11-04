using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Vixly.Shared.Hosting;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: VixlyEdgeHostTests.cs
//
// PURPOSE:
// The company address forwarded by the Cloudflare Worker is used
// only with the Worker's secret, only for the hosts allowed, and
// only from Cloudflare (side-by-side rollout on vixly.ltd).
//
// RESPONSIBILITIES:
// - A small host with the shared forwarded-headers set-up that
//   echoes the address it sees
//
// IMPORTANT NOTES:
// - The peer is set to a Cloudflare address unless a test asks
//   for another
// =========================================================
public sealed class VixlyEdgeHostTests
{
    private const string Secret = "edge-secret-for-tests-0123456789abcdef";

    [Fact]
    public async Task WorkerWithItsSecret_ForwardsTheCompanyAddress()
    {
        await using WebApplication app = await StartAsync(edge: true);

        Assert.Equal("galeo.vixly.ltd|https|", await AskAsync(app, "galeo.vixly.ltd", Secret));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("wrong-secret-0123456789abcdef-wrong")]
    public async Task WithoutTheSecret_TheForwardedAddressIsIgnored(string? proof)
    {
        await using WebApplication app = await StartAsync(edge: true);

        Assert.Equal("v4-origin.example|https|", await AskAsync(app, "galeo.vixly.ltd", proof));
    }

    [Fact]
    public async Task AnAddressOutsideTheAllowedHosts_IsIgnored()
    {
        await using WebApplication app = await StartAsync(edge: true);

        Assert.Equal("v4-origin.example|https|", await AskAsync(app, "evil.example.com", Secret));
    }

    [Fact]
    public async Task NotFromCloudflare_IsIgnoredEvenWithTheSecret()
    {
        await using WebApplication app = await StartAsync(edge: true, peer: IPAddress.Parse("203.0.113.9"));

        Assert.Equal("v4-origin.example|https|", await AskAsync(app, "galeo.vixly.ltd", Secret));
    }

    [Fact]
    public async Task WithoutEdgeSettings_NothingIsForwarded()
    {
        await using WebApplication app = await StartAsync(edge: false);

        Assert.Equal("v4-origin.example|https|", await AskAsync(app, "galeo.vixly.ltd", Secret));
    }

    private static async Task<string> AskAsync(WebApplication app, string forwardedHost, string? proof)
    {
        using HttpClient client = new(app.GetTestServer().CreateHandler()) { BaseAddress = new Uri("https://v4-origin.example/") };
        using HttpRequestMessage request = new(HttpMethod.Get, "/echo");
        request.Headers.Add("X-Forwarded-Host", forwardedHost);
        request.Headers.Add("CF-Connecting-IP", "198.51.100.7");

        if (proof is not null)
        {
            request.Headers.Add(VixlyEdgeHost.SecretHeader, proof);
        }

        HttpResponseMessage response = await client.SendAsync(request);

        return await response.Content.ReadAsStringAsync();
    }

    private static async Task<WebApplication> StartAsync(bool edge, IPAddress? peer = null)
    {
        // Production lists the company addresses and the origin's own name (host filtering checks the address as received).
        List<string> args = new() { "--Hosting:TrustedProxyNetworks:0=173.245.48.0/20", "--AllowedHosts=vixly.ltd;*.vixly.ltd;v4-origin.example" };

        if (edge)
        {
            args.Add("--Hosting:EdgeHosts:0=vixly.ltd");
            args.Add("--Hosting:EdgeHosts:1=*.vixly.ltd");
            args.Add("--Hosting:EdgeSecret=" + Secret);
        }

        WebApplicationBuilder builder = WebApplication.CreateBuilder(args.ToArray());
        builder.WebHost.UseTestServer();
        builder.Services.AddVixlyForwardedHeaders(builder.Configuration);
        WebApplication app = builder.Build();
        IPAddress from = peer ?? IPAddress.Parse("173.245.48.10");
        app.Use((http, next) =>
        {
            http.Connection.RemoteIpAddress = from;
            return next(http);
        });
        app.UseVixlyForwardedHeaders();
        app.MapGet("/echo", (HttpContext http) => $"{http.Request.Host.Host}|{http.Request.Scheme}|{http.Request.Headers[VixlyEdgeHost.SecretHeader]}");
        await app.StartAsync();

        return app;
    }
}
