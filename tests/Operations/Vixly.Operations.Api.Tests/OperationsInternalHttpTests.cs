using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Vixly.Contracts.Operations.Internal;

namespace Vixly.Operations.Api.Tests;

// =========================================================
// FILE: OperationsInternalHttpTests.cs
//
// PURPOSE:
// Proves the Operations.Api caller check and the tenant lookup (Slice 0, D-F4).
//
// RESPONSIBILITIES:
// - Refuse unknown hosts, missing, forged, expired, replayed and wrong-scope tokens
// - Answer which tenant a host belongs to; customers never see a tenant being set up
//
// IMPORTANT NOTES:
// - Runs the real pipeline on a TestServer
// =========================================================
public sealed class OperationsInternalHttpTests : IAsyncLifetime
{
    private OperationsApiTestHost _host = null!;

    public async Task InitializeAsync() => _host = await OperationsApiTestHost.StartAsync();

    public async Task DisposeAsync() => await _host.DisposeAsync();

    [Fact]
    public async Task UnknownHost_IsNotFound()
    {
        HttpResponseMessage response = await Send("northwind", _host.Customer.Sign(), _host.Client("vixly-api.techcrafters.ltd"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UnknownEndpoint_IsNotFound()
    {
        using HttpRequestMessage request = new(HttpMethod.Get, "internal/v1/nothing");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _host.Customer.Sign());

        Assert.Equal(HttpStatusCode.NotFound, (await _host.Client().SendAsync(request)).StatusCode);
    }

    [Fact]
    public async Task NoToken_IsRefused()
    {
        HttpResponseMessage response = await Send("northwind", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("ops.internal.token_invalid", await Code(response));
    }

    [Fact]
    public async Task ExpiredToken_IsRefused()
    {
        string token = _host.Customer.Sign(issued: DateTimeOffset.UtcNow.AddMinutes(-5));

        Assert.Equal(HttpStatusCode.Unauthorized, (await Send("northwind", token)).StatusCode);
    }

    [Fact]
    public async Task TokenForAnotherAudience_IsRefused()
    {
        string token = _host.Customer.Sign(audience: "vixly-platform-internal");

        Assert.Equal(HttpStatusCode.Unauthorized, (await Send("northwind", token)).StatusCode);
    }

    [Fact]
    public async Task TooLongLife_IsRefused()
    {
        string token = _host.Customer.Sign(lifetime: TimeSpan.FromMinutes(10));

        Assert.Equal(HttpStatusCode.Unauthorized, (await Send("northwind", token)).StatusCode);
    }

    [Fact]
    public async Task ReplayedToken_IsRefused()
    {
        _host.Platform.Tenant("northwind", Guid.NewGuid(), "active");
        string token = _host.Customer.Sign();

        Assert.Equal(HttpStatusCode.OK, (await Send("northwind", token)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Send("northwind", token)).StatusCode);
    }

    [Fact]
    public async Task ForgedSignature_IsRefused()
    {
        using CallerTokenSigner forger = new("customer");

        Assert.Equal(HttpStatusCode.Unauthorized, (await Send("northwind", forger.Sign())).StatusCode);
    }

    [Fact]
    public async Task WrongScope_IsForbidden()
    {
        HttpResponseMessage response = await Send("northwind", _host.Other.Sign());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("ops.internal.scope_not_allowed", await Code(response));
    }

    [Fact]
    public async Task LiveTenant_IsFound()
    {
        Guid tenant = Guid.NewGuid();
        _host.Platform.Tenant("northwind", tenant, "active", "read_only", "fr");

        HttpResponseMessage response = await Send("northwind", _host.Customer.Sign());
        OperationsTenantHostDto? dto = await response.Content.ReadFromJsonAsync<OperationsTenantHostDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        Assert.Equal(new OperationsTenantHostDto(true, tenant, "live", "read_only", "northwind", "fr"), dto);
    }

    [Theory]
    [InlineData("draft")]
    [InlineData("pending_activation")]
    public async Task TenantBeingSetUp_ReadsAsNotFound_ForCustomers(string status)
    {
        _host.Platform.Tenant("newco", Guid.NewGuid(), status);

        OperationsTenantHostDto? dto = await (await Send("newco", _host.Customer.Sign())).Content.ReadFromJsonAsync<OperationsTenantHostDto>();

        Assert.False(dto!.Found);
        Assert.Null(dto.TenantId);
        Assert.Equal("not_found", dto.State);
    }

    [Fact]
    public async Task SuspendedTenant_SaysSo()
    {
        _host.Platform.Tenant("paused", Guid.NewGuid(), "suspended");

        OperationsTenantHostDto? dto = await (await Send("paused", _host.Customer.Sign())).Content.ReadFromJsonAsync<OperationsTenantHostDto>();

        Assert.True(dto!.Found);
        Assert.Equal("suspended", dto.State);
    }

    [Fact]
    public async Task PlatformDown_IsUnavailable_NotNotFound()
    {
        _host.Platform.Down = true;

        HttpResponseMessage response = await Send("northwind", _host.Customer.Sign());

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("ops.common.platform_unavailable", await Code(response));
    }

    private async Task<HttpResponseMessage> Send(string slug, string? token, HttpClient? client = null)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, "internal/v1/tenants/by-host/" + slug);

        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        HttpResponseMessage response = await (client ?? _host.Client()).SendAsync(request);
        await response.Content.LoadIntoBufferAsync();
        return response;
    }

    private static async Task<string?> Code(HttpResponseMessage response)
    {
        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.TryGetProperty("code", out JsonElement code) ? code.GetString() : null;
    }
}
