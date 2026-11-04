using System.Net;

namespace Vixly.Customer.Web.Tests;

// =========================================================
// FILE: CustomerPortalHttpTests.cs
//
// PURPOSE:
// Proves how the customer portal answers each host and path (Slice 0, D-F2, D-F4, D-F5).
//
// RESPONSIBILITIES:
// - Serve only under /customer
// - 404 for unknown hosts, 403 for suspended tenants, 503 when Operations is down
// - Sign every call to Operations.Api as the caller customer
//
// IMPORTANT NOTES:
// - Runs the real pipeline on a TestServer
// =========================================================
public sealed class CustomerPortalHttpTests : IAsyncLifetime
{
    private CustomerWebTestHost _host = null!;

    public async Task InitializeAsync() => _host = await CustomerWebTestHost.StartAsync();

    public async Task DisposeAsync() => await _host.DisposeAsync();

    [Fact]
    public async Task LiveTenant_ShowsThePortal()
    {
        _host.Tenant("northwind", "live");

        HttpResponseMessage response = await _host.Client("northwind.techcrafters.ltd").GetAsync("/customer/");
        string html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<base href=\"/customer/\"", html, StringComparison.Ordinal);
        Assert.Contains("Customer portal", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PathOutsideCustomer_IsNotServed()
    {
        _host.Tenant("northwind", "live");

        HttpResponseMessage response = await _host.Client("northwind.techcrafters.ltd").GetAsync("/");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, _host.Operations.Calls);
    }

    [Theory]
    [InlineData("nobody.techcrafters.ltd")]
    [InlineData("northwind.example.com")]
    public async Task UnknownHost_IsNotFound(string host)
    {
        HttpResponseMessage response = await _host.Client(host).GetAsync("/customer/");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Page not found", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnknownPath_IsNotFound()
    {
        _host.Tenant("northwind", "live");

        HttpResponseMessage response = await _host.Client("northwind.techcrafters.ltd").GetAsync("/customer/nothing-here");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task StatusPages_AreLimitedPerAddress()
    {
        HttpClient client = _host.Client("nobody.techcrafters.ltd");

        for (int request = 0; request < 60; request++)
        {
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/customer/")).StatusCode);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.GetAsync("/customer/")).StatusCode);
    }

    [Fact]
    public async Task SuspendedTenant_IsNotAvailable()
    {
        _host.Tenant("paused", "suspended");

        HttpResponseMessage response = await _host.Client("paused.techcrafters.ltd").GetAsync("/customer/");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OperationsDown_AsksToTryAgain()
    {
        _host.Operations.Down = true;

        HttpResponseMessage response = await _host.Client("northwind.techcrafters.ltd").GetAsync("/customer/");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Calls_AreSignedAsTheCustomerCaller_AndCached()
    {
        _host.Tenant("northwind", "live");
        HttpClient client = _host.Client("northwind.techcrafters.ltd");

        await client.GetAsync("/customer/");
        await client.GetAsync("/customer/");

        // One tenant lookup and one brand call: both are cached for the second page.
        Assert.Equal(2, _host.Operations.Claims.Count);

        foreach (System.Text.Json.JsonElement claims in _host.Operations.Claims)
        {
            Assert.Equal("customer", claims.GetProperty("iss").GetString());
            Assert.Equal("vixly-operations-internal", claims.GetProperty("aud").GetString());
            Assert.Equal(60, claims.GetProperty("exp").GetInt64() - claims.GetProperty("iat").GetInt64());
        }
    }

    [Fact]
    public async Task StaffCookies_AreIgnored()
    {
        _host.Tenant("northwind", "live");
        using HttpRequestMessage request = new(HttpMethod.Get, "/customer/");
        request.Headers.Add("Cookie", "__Host-vx4-ops=forged; __Host-vx4-ops-lang=c=fr|uic=fr");

        HttpResponseMessage response = await _host.Client("northwind.techcrafters.ltd").SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("lang=\"en\"", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task CustomerLanguageCookie_IsRead()
    {
        _host.Tenant("northwind", "live");
        using HttpRequestMessage request = new(HttpMethod.Get, "/customer/");
        request.Headers.Add("Cookie", "__Secure-vx4-customer-lang=c=es|uic=es");

        HttpResponseMessage response = await _host.Client("northwind.techcrafters.ltd").SendAsync(request);

        Assert.Contains("Portal de clientes", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }
}
