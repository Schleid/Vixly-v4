using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Vixly.Contracts.Platform.Common;
using Vixly.Contracts.Platform.Internal.Portal;

namespace Vixly.Platform.Api.Tests;

// =========================================================
// FILE: PlatformInternalHttpTests.cs
//
// PURPOSE:
// Proves the internal channel guards over HTTP: host, service token, one use, scope and tenant (Slice 9, D-K1, D-K2, D-K7).
//
// RESPONSIBILITIES:
// - Internal routes exist only on the internal host
// - Every token rule refuses with the same code
// - Scopes and the tenant claim are checked before any handler
//
// IMPORTANT NOTES:
// - A valid call is proven with a batch of 101 addresses: refused by the handler, before the database
// =========================================================
public sealed class PlatformInternalHttpTests : IDisposable
{
    private const string InternalHost = "platform-internal.test";

    private readonly PlatformServiceTokenSigner _operations = new("operations");

    private readonly PlatformServiceTokenSigner _edge = new("edge", "e1");

    private readonly PlatformServiceTokenSigner _stranger = new("operations");

    public void Dispose()
    {
        _operations.Dispose();
        _edge.Dispose();
        _stranger.Dispose();
    }

    private async Task<(WebApplication App, HttpClient Client)> StartAsync()
    {
        (WebApplication app, HttpClient client) = await PlatformApiTestHost.StartAsync(new Dictionary<string, string?>
        {
            ["AllowedHosts"] = PlatformApiTestHost.Host + ";" + InternalHost,
            ["PlatformInternal:Hosts:0"] = InternalHost,
            ["PlatformInternal:Callers:operations:Scopes:0"] = "tenant",
            ["PlatformInternal:Callers:operations:Scopes:1"] = "events",
            ["PlatformInternal:Callers:operations:Scopes:2"] = "jobs",
            ["PlatformInternal:Callers:operations:Keys:k1"] = _operations.PublicKey,
            ["PlatformInternal:Callers:edge:Scopes:0"] = "portal",
            ["PlatformInternal:Callers:edge:Scopes:1"] = "events",
            ["PlatformInternal:Callers:edge:Keys:e1"] = _edge.PublicKey
        });
        client.BaseAddress = new Uri($"https://{InternalHost}/");
        return (app, client);
    }

    private static HttpRequestMessage Batch(string? token, int count = 101)
    {
        HttpRequestMessage request = new(HttpMethod.Post, "internal/v1/portal/batch")
        {
            Content = JsonContent.Create(new PlatformPortalAccessBatchBody(Enumerable.Range(0, count).Select(index => "slug-" + index).ToArray()))
        };

        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return request;
    }

    private static async Task<string?> CodeAsync(HttpResponseMessage response)
    {
        return (await response.Content.ReadFromJsonAsync<PlatformApiError>())?.Code;
    }

    [Fact]
    public async Task InternalRoutes_DoNotExist_OnThePublicHost_OrForUnknownPaths()
    {
        (WebApplication app, HttpClient client) = await StartAsync();
        await using WebApplication _ = app;

        HttpRequestMessage request = Batch(_edge.Sign());
        request.RequestUri = new Uri($"https://{PlatformApiTestHost.Host}/internal/v1/portal/batch");
        Assert.Equal(HttpStatusCode.NotFound, (await client.SendAsync(request)).StatusCode);

        HttpRequestMessage unknown = new(HttpMethod.Get, "internal/v1/nothing-here");
        unknown.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _edge.Sign());
        Assert.Equal(HttpStatusCode.NotFound, (await client.SendAsync(unknown)).StatusCode);
    }

    [Fact]
    public async Task AValidToken_ReachesTheHandler_AndIsAcceptedOnlyOnce()
    {
        (WebApplication app, HttpClient client) = await StartAsync();
        await using WebApplication _ = app;
        string token = _edge.Sign();

        HttpResponseMessage first = await client.SendAsync(Batch(token));
        Assert.Equal(HttpStatusCode.BadRequest, first.StatusCode);
        PlatformApiError? error = await first.Content.ReadFromJsonAsync<PlatformApiError>();
        Assert.Equal("platform.internal.too_many_items", error?.Code);
        Assert.Equal("100", error?.Arguments?["max"]);
        Assert.False(string.IsNullOrEmpty(error?.CorrelationId));
        Assert.Equal(error?.CorrelationId, first.Headers.GetValues("X-Correlation-Id").Single());
        Assert.Equal("no-store", first.Headers.CacheControl?.ToString());

        HttpResponseMessage replay = await client.SendAsync(Batch(token));
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
        Assert.Equal("platform.internal.token_invalid", await CodeAsync(replay));
    }

    [Fact]
    public async Task EveryBrokenToken_IsRefusedWithOneCode()
    {
        (WebApplication app, HttpClient client) = await StartAsync();
        await using WebApplication _ = app;
        string good = _edge.Sign();
        string[] parts = good.Split('.');
        string[] broken =
        {
            string.Empty,
            "not-a-token",
            _stranger.Sign(),
            _edge.Sign(audience: "someone-else"),
            _edge.Sign(lifetime: TimeSpan.FromMinutes(10)),
            _edge.Sign(issued: DateTimeOffset.UtcNow.AddMinutes(-10)),
            _edge.Sign(jti: "short"),
            parts[0] + "." + parts[1] + "." + parts[2][..^4] + "AAAA",
            new PlatformServiceTokenSigner("edge", "unknown-key").Sign()
        };

        foreach (string token in broken)
        {
            HttpResponseMessage response = await client.SendAsync(Batch(token.Length == 0 ? null : token));
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal("platform.internal.token_invalid", await CodeAsync(response));
            Assert.Contains("Bearer", response.Headers.WwwAuthenticate.ToString(), StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Scopes_AndTheTenantClaim_AreCheckedBeforeAnyHandler()
    {
        (WebApplication app, HttpClient client) = await StartAsync();
        await using WebApplication _ = app;

        // Operations may not resolve portals; Edge may not read a tenant.
        Assert.Equal("platform.internal.scope_not_allowed", await CodeAsync(await client.SendAsync(Batch(_operations.Sign()))));

        HttpRequestMessage edgeTenant = new(HttpMethod.Get, "internal/v1/tenant");
        edgeTenant.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _edge.Sign(new Dictionary<string, object?> { ["tid"] = Guid.NewGuid().ToString() }));
        HttpResponseMessage refused = await client.SendAsync(edgeTenant);
        Assert.Equal(HttpStatusCode.Forbidden, refused.StatusCode);
        Assert.Equal("platform.internal.scope_not_allowed", await CodeAsync(refused));

        HttpRequestMessage noTenant = new(HttpMethod.Get, "internal/v1/tenant");
        noTenant.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _operations.Sign());
        noTenant.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("fr"));
        HttpResponseMessage missing = await client.SendAsync(noTenant);
        Assert.Equal(HttpStatusCode.Forbidden, missing.StatusCode);
        PlatformApiError? error = await missing.Content.ReadFromJsonAsync<PlatformApiError>();
        Assert.Equal("platform.internal.tenant_required", error?.Code);
        Assert.Contains("client", error?.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CompanyChange_IsForOperations_WithATenantClaim()
    {
        (WebApplication app, HttpClient client) = await StartAsync();
        await using WebApplication _ = app;

        // B12: only Operations sends a company change, always for one tenant.
        HttpRequestMessage edge = new(HttpMethod.Post, "internal/v1/tenant/company-change") { Content = JsonContent.Create(new { reason = "x" }) };
        edge.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _edge.Sign(new Dictionary<string, object?> { ["tid"] = Guid.NewGuid().ToString() }));
        Assert.Equal("platform.internal.scope_not_allowed", await CodeAsync(await client.SendAsync(edge)));

        HttpRequestMessage noTenant = new(HttpMethod.Get, "internal/v1/tenant/company-change");
        noTenant.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _operations.Sign());
        Assert.Equal("platform.internal.tenant_required", await CodeAsync(await client.SendAsync(noTenant)));
    }
}
