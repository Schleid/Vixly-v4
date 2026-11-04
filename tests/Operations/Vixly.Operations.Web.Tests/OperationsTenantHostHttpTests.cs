using System.Net;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: OperationsTenantHostHttpTests.cs
//
// PURPOSE:
// Proves how Operations.Web answers each host name before any database work (Slice 0, D-F2).
//
// RESPONSIBILITIES:
// - 404 for unknown hosts, 403 for suspended tenants, 503 when Platform is down
// - Tenants being set up are sent to /setup
// - Status pages are limited per address
//
// IMPORTANT NOTES:
// - Runs the real pipeline on a TestServer with an unreachable database
// =========================================================
public sealed class OperationsTenantHostHttpTests : IAsyncLifetime
{
    private OperationsWebTestHost _host = null!;

    public async Task InitializeAsync() => _host = await OperationsWebTestHost.StartAsync();

    public async Task DisposeAsync() => await _host.DisposeAsync();

    [Theory]
    [InlineData("nobody.techcrafters.ltd")]
    [InlineData("northwind.example.com")]
    [InlineData("a.b.techcrafters.ltd")]
    public async Task UnknownHost_IsNotFound(string host)
    {
        HttpResponseMessage response = await _host.Client(host).GetAsync("/");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Page not found", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnknownHost_NeverReachesSignIn()
    {
        HttpResponseMessage response = await _host.Client("nobody.techcrafters.ltd").GetAsync("/invitation?token=x");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task StatusPages_AreLimitedPerAddress()
    {
        HttpClient client = _host.Client("nobody.techcrafters.ltd");

        for (int request = 0; request < 60; request++)
        {
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/")).StatusCode);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.GetAsync("/")).StatusCode);
    }

    [Fact]
    public async Task SuspendedTenant_IsNotAvailable()
    {
        _host.Platform.Tenant("paused", Guid.NewGuid(), "suspended");

        HttpResponseMessage response = await _host.Client("paused.techcrafters.ltd").GetAsync("/");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("Service not available", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task PlatformDown_AsksToTryAgain()
    {
        _host.Platform.Down = true;

        HttpResponseMessage response = await _host.Client("northwind.techcrafters.ltd").GetAsync("/");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Theory]
    [InlineData("draft", "/")]
    [InlineData("pending_activation", "/workspace")]
    public async Task TenantBeingSetUp_GoesToSetup(string status, string path)
    {
        _host.Platform.Tenant("newco", Guid.NewGuid(), status);

        HttpResponseMessage response = await _host.Client("newco.techcrafters.ltd").GetAsync(path);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/setup", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task KeepAlive_WithoutASession_GoesToStaffSignIn()
    {
        _host.Platform.Tenant("northwind", Guid.NewGuid(), "active");

        HttpResponseMessage response = await _host.Client("northwind.techcrafters.ltd").GetAsync("/session/keepalive");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/staff", response.Headers.Location?.OriginalString);
    }
}
