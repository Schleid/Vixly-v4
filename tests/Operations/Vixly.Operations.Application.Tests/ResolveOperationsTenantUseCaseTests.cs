using Vixly.Operations.Application.Platform;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Tenancy;
using Vixly.Operations.Application.Tests.Fakes;
using Vixly.Operations.Application.UseCases.Tenancy.ResolveOperationsTenant;

namespace Vixly.Operations.Application.Tests;

// =========================================================
// FILE: ResolveOperationsTenantUseCaseTests.cs
//
// PURPOSE:
// Proves how a host name becomes a tenant state (Slice 0, D-F2).
//
// RESPONSIBILITIES:
// - Map every Platform status
// - Refuse bad slugs without asking Platform
// - Report Platform being down as an error, never as not found
//
// IMPORTANT NOTES:
// - No database
// =========================================================
public sealed class ResolveOperationsTenantUseCaseTests
{
    private readonly FakePlatformClient _platform = new();

    [Theory]
    [InlineData("active", OperationsTenantState.Live)]
    [InlineData("suspended", OperationsTenantState.Suspended)]
    [InlineData("draft", OperationsTenantState.SettingUp)]
    [InlineData("pending_activation", OperationsTenantState.SettingUp)]
    [InlineData("deactivated", OperationsTenantState.NotFound)]
    public async Task Status_MapsToAState(string status, OperationsTenantState expected)
    {
        Guid tenant = Guid.NewGuid();
        _platform.Portals["northwind"] = new OperationsPortalAnswer(true, tenant, status, "full", "northwind", "fr", "accepted");

        ResolveOperationsTenantResponse response = await Execute("Northwind");

        Assert.True(response.Succeeded);
        Assert.Equal(expected, response.Host!.State);
        Assert.Equal(expected == OperationsTenantState.NotFound ? null : tenant, response.Host.TenantId);
    }

    [Fact]
    public async Task Access_IsKept()
    {
        _platform.Portals["northwind"] = new OperationsPortalAnswer(true, Guid.NewGuid(), "active", "payment_required", "northwind", "en", "accepted");

        ResolveOperationsTenantResponse response = await Execute("northwind");

        Assert.Equal(OperationsTenantAccess.PaymentRequired, response.Host!.Access);
    }

    [Theory]
    [InlineData("")]
    [InlineData("-bad")]
    [InlineData("a.b")]
    [InlineData("north-")]
    public async Task BadSlug_IsNotFound_WithoutAskingPlatform(string slug)
    {
        ResolveOperationsTenantResponse response = await Execute(slug);

        Assert.Equal(OperationsTenantState.NotFound, response.Host!.State);
        Assert.Equal(0, _platform.PortalCalls);
    }

    [Fact]
    public async Task UnknownSlug_IsNotFound()
    {
        ResolveOperationsTenantResponse response = await Execute("nobody");

        Assert.Equal(OperationsTenantState.NotFound, response.Host!.State);
    }

    [Fact]
    public async Task PlatformDown_IsAnError()
    {
        _platform.Down = true;

        ResolveOperationsTenantResponse response = await Execute("northwind");

        Assert.False(response.Succeeded);
        Assert.Equal("ops.common.platform_unavailable", response.Error!.Code);
    }

    private Task<ResolveOperationsTenantResponse> Execute(string slug)
    {
        return new ResolveOperationsTenantUseCase(new FakeTenantDirectory(_platform))
            .ExecuteAsync(OperationsAuthorizationContext.Host, new ResolveOperationsTenantRequest { Slug = slug }, CancellationToken.None);
    }
}
