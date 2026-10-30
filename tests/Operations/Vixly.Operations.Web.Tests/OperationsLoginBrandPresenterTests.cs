using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Tenancy;
using Vixly.Operations.Application.UseCases.Site.GetOperationsPublicSite;
using Vixly.Operations.Domain.Branding;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Web.Features.SignIn.LoginBrand;
using Vixly.Operations.Web.Tenancy;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: OperationsLoginBrandPresenterTests.cs
//
// PURPOSE:
// Verify staff-entry branding without a database or live tenant.
//
// RESPONSIBILITIES:
// - Prove published-only selection and safe fallback
// - Prove the resolved tenant is used instead of request input
//
// IMPORTANT NOTES:
// - Application behavior is exercised through its real public-site use case
// - Interface probes fail on unexpected calls
// =========================================================
public sealed class OperationsLoginBrandPresenterTests
{
    [Fact]
    public async Task PublishedLogo_WinsOverUnpublishedDraft_AndUsesResolvedTenant()
    {
        Guid tenant = Guid.NewGuid();
        Guid published = Guid.NewGuid();
        var brand = OperationsTenantBrand.Create(tenant, "Tenant A", DateTimeOffset.UtcNow);
        brand.ChangeDraft(brand.Draft.WithImage(OperationsSiteImageKind.Logo, published), DateTimeOffset.UtcNow);
        brand.Publish(DateTimeOffset.UtcNow);
        brand.ChangeDraft(brand.Draft.WithImage(OperationsSiteImageKind.Logo, Guid.NewGuid()), DateTimeOffset.UtcNow);
        Guid? observedTenant = null;
        using var presenter = Create(tenant, brand, observeTenant: id => observedTenant = id);

        await presenter.LoadAsync();

        Assert.Equal("/site/image/" + published.ToString("N"), presenter.State.LogoUrl);
        Assert.Equal(tenant, observedTenant);
    }

    [Fact]
    public async Task DraftLogoOnly_KeepsInitialsFallback()
    {
        Guid tenant = Guid.NewGuid();
        var brand = OperationsTenantBrand.Create(tenant, "Tenant A", DateTimeOffset.UtcNow);
        brand.ChangeDraft(brand.Draft.WithImage(OperationsSiteImageKind.Logo, Guid.NewGuid()), DateTimeOffset.UtcNow);
        using var presenter = Create(tenant, brand);
        await presenter.LoadAsync();
        Assert.Null(presenter.State.LogoUrl);
    }

    [Fact]
    public async Task MissingBrand_KeepsInitialsFallback()
    {
        using var presenter = Create(Guid.NewGuid(), null);
        await presenter.LoadAsync();
        Assert.Null(presenter.State.LogoUrl);
        Assert.Null(presenter.FailureReference);
    }

    [Fact]
    public async Task Setup_DoesNotReadPublicSite()
    {
        using var presenter = Create(Guid.NewGuid(), null, setup: true, failRead: true);
        await presenter.LoadAsync();
        Assert.Null(presenter.State.LogoUrl);
        Assert.Null(presenter.FailureReference);
    }

    [Fact]
    public async Task BrandingFailure_IsLoggedByPresenter_AndKeepsFormFallback()
    {
        using var presenter = Create(Guid.NewGuid(), null, failRead: true);
        await presenter.LoadAsync();
        Assert.Null(presenter.State.LogoUrl);
        Assert.NotNull(presenter.FailureReference);
    }

    private static OperationsLoginBrandPresenter Create(Guid tenant, OperationsTenantBrand? brand, bool setup = false, bool failRead = false, Action<Guid>? observeTenant = null)
    {
        var http = new DefaultHttpContext();
        http.Items[OperationsTenantMiddleware.HostItem] = new OperationsTenantHost(setup ? OperationsTenantState.SettingUp : OperationsTenantState.Live, tenant, OperationsTenantAccess.None, "tenant-a", "en");
        http.Request.QueryString = new QueryString("?tenantId=" + Guid.NewGuid());
        var site = Probe<IOperationsSiteRepository>.Create((method, _) => method.Name == "FindBrandAsync"
            ? failRead ? throw new InvalidOperationException("Test failure") : Task.FromResult(brand)
            : throw new InvalidOperationException("Unexpected site operation"));
        var locations = Probe<IOperationsLocationRepository>.Create((method, _) => method.Name switch
        {
            "ListBranchesAsync" => Task.FromResult<IReadOnlyList<OperationsBranch>>(Array.Empty<OperationsBranch>()),
            "ListWarehousesAsync" => Task.FromResult<IReadOnlyList<OperationsWarehouse>>(Array.Empty<OperationsWarehouse>()),
            "ListContactsAsync" => Task.FromResult<IReadOnlyList<OperationsCountryContact>>(Array.Empty<OperationsCountryContact>()),
            _ => throw new InvalidOperationException("Unexpected location operation")
        });
        var users = Probe<IOperationsUserRepository>.Create((_, _) => throw new InvalidOperationException("Public branding must not read users"));
        var features = Probe<IOperationsPlanFeatures>.Create((_, args) =>
        {
            observeTenant?.Invoke((Guid)args![0]!);
            return Task.FromResult(OperationsPlanFeatures.All);
        });
        return new(new GetOperationsPublicSiteUseCase(site, locations, new OperationsOwnerCheck(users), features), new HttpContextAccessor { HttpContext = http }, NullLogger<OperationsLoginBrandPresenter>.Instance);
    }

    public class Probe<T> : DispatchProxy where T : class
    {
        private Func<MethodInfo, object?[]?, object?> _invoke = null!;

        public static T Create(Func<MethodInfo, object?[]?, object?> invoke)
        {
            T proxy = Create<T, Probe<T>>();
            ((Probe<T>)(object)proxy)._invoke = invoke;
            return proxy;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => _invoke(targetMethod!, args);
    }
}
