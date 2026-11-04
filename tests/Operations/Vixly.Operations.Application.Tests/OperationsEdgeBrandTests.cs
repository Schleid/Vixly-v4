using Vixly.Operations.Application.Branding;
using Vixly.Operations.Application.Platform;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Tests.Fakes;
using Vixly.Operations.Domain.Branding;
using Vixly.Operations.Domain.Messaging;

namespace Vixly.Operations.Application.Tests;

// =========================================================
// FILE: OperationsEdgeBrandTests.cs
//
// PURPOSE:
// Proves a company's brand goes to the Cloudflare Worker for its maintenance page (owner, 2026-10-23 W-M2 and 2026-10-28).
//
// RESPONSIBILITIES:
// - The published name, main colour and logo, with the company's default language and portal address name
// - A logo over 100 KB is left out (the page shows the name)
// - The handler finds the address name with Platform; an unknown company sends nothing
//
// IMPORTANT NOTES:
// - The Worker is a recording fake over the in-memory site store
// =========================================================
public sealed class OperationsEdgeBrandTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 29, 6, 0, 0, TimeSpan.Zero);

    private readonly Guid _tenantId = Guid.NewGuid();

    private readonly FakeSiteStore _site = new();

    private readonly RecordingEdge _edge = new();

    private readonly OperationsTenantAccount _account;

    public OperationsEdgeBrandTests()
    {
        _account = new OperationsTenantAccount(_tenantId, "northwind", "Northwind Freight LLC", "Northwind Freight", "FR", "Grace Hopper", null);
    }

    [Fact]
    public async Task SendsThePublishedNameColourAndLogo_InTheCompanyLanguage()
    {
        OperationsSiteImage logo = Image(2_000);
        Publish(logo);

        Assert.Equal(OperationsEdgeSendResult.Sent, await new OperationsEdgeBrands(_site, _edge).SendAsync(_account, CancellationToken.None));

        OperationsEdgeBrand brand = Assert.Single(_edge.Brands);
        Assert.Equal("northwind", brand.Slug);
        Assert.Equal("Northwind Cargo", brand.Name);
        Assert.Equal("#0F4C5C", brand.Colour);
        Assert.Equal("fr", brand.Language);
        Assert.Equal(logo.Content, brand.Logo);
        Assert.Equal("image/png", brand.LogoContentType);
    }

    [Fact]
    public async Task ALogoOver100KB_IsLeftOut_AndNoBrandUsesThePlatformName()
    {
        Publish(Image(OperationsEdgeBrands.MaxLogoBytes + 1));
        await new OperationsEdgeBrands(_site, _edge).SendAsync(_account with { DefaultLanguage = "de" }, CancellationToken.None);
        Assert.Null(_edge.Brands[0].Logo);
        Assert.Null(_edge.Brands[0].LogoContentType);
        Assert.Equal("en", _edge.Brands[0].Language);

        _site.Brand = null;
        await new OperationsEdgeBrands(_site, _edge).SendAsync(_account, CancellationToken.None);
        Assert.Equal("Northwind Freight", _edge.Brands[1].Name);
        Assert.Null(_edge.Brands[1].Colour);
    }

    [Fact]
    public async Task TheHandler_FindsTheAddressWithPlatform_AndSkipsAnUnknownCompany()
    {
        FakePlatformClient platform = new();
        OperationsEdgeBrandHandler handler = new(platform, new OperationsEdgeBrands(_site, _edge));
        OperationsOutboxMessage message = OperationsOutboxMessage.Create(OperationsEdgeBrandChanged.MessageType, _tenantId, Guid.NewGuid(), "{\"publishedVersion\":2}", Now);

        Assert.Equal(OperationsEdgeBrandChanged.MessageType, handler.MessageType);
        await handler.HandleAsync(message, CancellationToken.None);
        Assert.Empty(_edge.Brands);

        platform.Accounts[_tenantId] = _account;
        await handler.HandleAsync(message, CancellationToken.None);
        Assert.Equal("northwind", Assert.Single(_edge.Brands).Slug);

        platform.Down = true;
        await Assert.ThrowsAsync<OperationsPlatformUnavailableException>(() => handler.HandleAsync(message, CancellationToken.None));
    }

    private void Publish(OperationsSiteImage logo)
    {
        OperationsTenantBrand brand = OperationsTenantBrand.Create(_tenantId, "Northwind Cargo", Now);
        brand.ChangeDraft(brand.Draft.WithImage(OperationsSiteImageKind.Logo, logo.Id).With(
            "Northwind Cargo", OperationsSitePreset.Harbour, "#0F4C5C", "#E36414", OperationsSiteServices.All, true, false, (null, null, null, null, null, null)), Now);
        brand.Publish(Now);
        _site.Brand = brand;
        _site.AddImage(logo);
    }

    private OperationsSiteImage Image(int bytes) =>
        OperationsSiteImage.Create(_tenantId, OperationsSiteImageKind.Logo, "image/png", new byte[bytes], 200, 60, new string('a', 64), Now);

    private sealed class RecordingEdge : IOperationsEdgeClient
    {
        public List<OperationsEdgeBrand> Brands { get; } = new();

        public Task<OperationsEdgeSendResult> SendBrandAsync(OperationsEdgeBrand brand, CancellationToken cancellationToken)
        {
            Brands.Add(brand);
            return Task.FromResult(OperationsEdgeSendResult.Sent);
        }
    }
}
