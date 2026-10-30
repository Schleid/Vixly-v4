using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Vixly.Platform.Application.UseCases.Tenants.ListPlatformTenants;
using Vixly.Platform.Web.Endpoints;
using Vixly.Platform.Web.Features.Dashboard;
using Vixly.Platform.Web.Features.Tenants;
using Vixly.Platform.Web.Security;

namespace Vixly.Platform.Web.Tests;

// =========================================================
// FILE: ControlOverviewUiTests.cs
//
// PURPOSE:
// Proves the real dashboard metric follows its authorized presenter.
//
// RESPONSIBILITIES:
// - Distinguish totals from the bounded five-row preview
// - Keep unavailable data out of metric values
//
// IMPORTANT NOTES:
// - Executes the existing use case over test ports, never a database
// =========================================================
public sealed class ControlOverviewUiTests : BunitContext
{
    private ControlTenantPreviewData Configure()
    {
        PresenterTestKit kit = new();
        ControlTenantPreviewData data = new();
        Services.AddLogging();
        Services.AddLocalization();
        Services.AddScoped<PlatformInteractiveRequest>();
        Services.AddTransient<PlatformErrorText>();
        Services.AddScoped(_ => new TenantListPresenter(
            new ListPlatformTenantsUseCase(kit, data, kit, TimeProvider.System),
            data, kit.Scope(), PresenterTestKit.Logger<TenantListPresenter>()));
        return data;
    }

    [Fact]
    public void MetricUsesTotal_WhilePreviewStaysBounded()
    {
        ControlTenantPreviewData data = Configure();
        data.Complete(57, 6);
        var view = Render<TenantOverview>();
        view.WaitForAssertion(() => Assert.Equal("57", view.Find(".vx-metric__value").TextContent));
        Assert.Equal(5, view.FindAll("tbody tr").Count);
        Assert.Equal(1, data.Reads);
        Assert.Equal("/tenants", view.Find(".vx-control-metric-link").GetAttribute("href"));
    }

    [Fact]
    public void LoadingThenEmpty_DoesNotShowInventedCounts()
    {
        ControlTenantPreviewData data = Configure();
        var view = Render<TenantOverview>();
        Assert.Equal("—", view.Find(".vx-metric__value").TextContent);
        Assert.Equal("true", view.Find("section").GetAttribute("aria-busy"));
        data.Complete(0);
        view.WaitForAssertion(() => Assert.Equal("0", view.Find(".vx-metric__value").TextContent));
        Assert.Single(view.FindAll(".vx-empty-state"));
    }

    [Fact]
    public void DeniedRead_ShowsErrorWithoutTenantRowsOrCount()
    {
        ControlTenantPreviewData data = Configure();
        data.Result.SetException(new UnauthorizedAccessException());
        var view = Render<TenantOverview>();
        view.WaitForAssertion(() => Assert.Single(view.FindAll("[role=alert]")));
        Assert.Equal("—", view.Find(".vx-metric__value").TextContent);
        Assert.Empty(view.FindAll("tbody tr"));
    }
    [Fact]
    public void ShellSearchTargetsDirectory_AndSkipLinkKeepsCurrentPage()
    {
        Services.AddLocalization();
        ComponentFactories.AddStub<Microsoft.AspNetCore.Components.Authorization.AuthorizeView>();
        Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().NavigateTo("/invoices?overdue=true");
        var view = Render<Vixly.Platform.Web.Components.Layout.MainLayout>();
        var search = view.Find("form[role=search]");
        Assert.Equal("get", search.GetAttribute("method"));
        Assert.Equal("/tenants", search.GetAttribute("action"));
        Assert.Equal("search", search.QuerySelector("input")!.GetAttribute("name"));
        Assert.False(search.QuerySelector("input")!.HasAttribute("readonly"));
        Assert.Equal("/invoices?overdue=true#vx-main-content", view.Find(".vx-skip-link").GetAttribute("href"));
        Assert.Empty(view.FindAll(".vx-sidebar__telemetry, .vx-topbar__status"));
    }

}
