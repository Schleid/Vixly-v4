using Microsoft.Extensions.DependencyInjection;
using Vixly.Operations.Application.Branding;
using Vixly.Operations.Application.Platform;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Branding;
using Vixly.Operations.Web.Hosting;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: OperationsEdgeBrandHttpTests.cs
//
// PURPOSE:
// Proves the nightly round sends every company's brand to the Cloudflare Worker (owner, 2026-10-28).
//
// RESPONSIBILITIES:
// - Every company with a brand, in its own tenant scope, with its portal address name from Platform
// - One company failing never stops the others; the round is recorded
//
// IMPORTANT NOTES:
// - Real database; the Worker is FakeEdgeClient
// =========================================================
public sealed class OperationsEdgeBrandHttpTests : IClassFixture<OperationsWebDatabase>, IAsyncLifetime
{
    private readonly OperationsWebDatabase _database;

    private OperationsWebTestHost _host = null!;

    public OperationsEdgeBrandHttpTests(OperationsWebDatabase database)
    {
        _database = database;
    }

    public async Task InitializeAsync()
    {
        _host = await OperationsWebTestHost.StartAsync(_database.ConnectionString);
    }

    public async Task DisposeAsync() => await _host.DisposeAsync();

    [SqlServerFact]
    public async Task TheNightlyRound_SendsEveryCompanyBrand_AndOneFailureStopsNoOther()
    {
        string first = "edge" + Guid.NewGuid().ToString("N")[..8];
        string second = "edge" + Guid.NewGuid().ToString("N")[..8];
        string broken = "edge" + Guid.NewGuid().ToString("N")[..8];
        await BrandAsync(first, "First Cargo", "fr");
        await BrandAsync(second, "Second Cargo", "es");
        await BrandAsync(broken, "Broken Cargo", "en");
        _host.Edge.Failing.Add(broken);

        int sent = await _host.Services.GetRequiredService<OperationsEdgeBrandWorker>().RunAsync(CancellationToken.None);

        Assert.True(sent >= 2);
        OperationsEdgeBrand one = Assert.Single(_host.Edge.Brands, brand => brand.Slug == first);
        Assert.Equal("First Cargo", one.Name);
        Assert.Equal("fr", one.Language);
        Assert.Equal("es", Assert.Single(_host.Edge.Brands, brand => brand.Slug == second).Language);
        Assert.DoesNotContain(_host.Edge.Brands, brand => brand.Slug == broken);
    }

    private async Task BrandAsync(string slug, string name, string language)
    {
        Guid tenant = Guid.NewGuid();
        _host.Platform.Tenant(slug, tenant, "active");
        _host.Platform.Accounts[tenant] = new OperationsTenantAccount(tenant, slug, name + " LLC", name, language, "Grace Hopper", "grace@" + slug + ".example");

        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
        scope.ServiceProvider.GetRequiredService<IOperationsBrandRepository>().Add(OperationsTenantBrand.Create(tenant, name, DateTimeOffset.UtcNow));
        Assert.True(await scope.ServiceProvider.GetRequiredService<IOperationsUnitOfWork>().SaveChangesAsync(CancellationToken.None));
    }
}
