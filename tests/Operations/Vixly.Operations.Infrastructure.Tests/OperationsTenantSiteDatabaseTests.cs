using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Branding;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Infrastructure.Persistence;
using Vixly.Operations.Infrastructure.Persistence.Repositories;
using Vixly.Operations.Infrastructure.Security;

namespace Vixly.Operations.Infrastructure.Tests;

// =========================================================
// FILE: OperationsTenantSiteDatabaseTests.cs
//
// PURPOSE:
// Proves the Slice 1B site and location tables against SQL Server, and the image check.
//
// RESPONSIBILITIES:
// - Draft and published brand, texts and images round-trip
// - Locations and contacts are invisible to another tenant; codes are unique per tenant
// - Images are recognized by their content; SVG and other files are refused
//
// IMPORTANT NOTES:
// - Runs against a throwaway SQL Server database (the image check needs none)
// =========================================================
public sealed class OperationsTenantSiteDatabaseTests : IClassFixture<OperationsDatabaseFixture>
{
    private static readonly DateTimeOffset Now = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);

    private readonly OperationsDatabaseFixture _database;

    public OperationsTenantSiteDatabaseTests(OperationsDatabaseFixture database)
    {
        _database = database;
    }

    [SqlServerFact]
    public async Task Brand_RoundTrips_WithDraftPublishedTextsAndImages()
    {
        Guid tenant = Guid.NewGuid();
        OperationsTenantBrand brand = OperationsTenantBrand.Create(tenant, "Northwind Freight", Now);
        OperationsSiteImage logo = OperationsSiteImage.Create(tenant, OperationsSiteImageKind.Logo, "image/png", new byte[] { 1, 2, 3 }, 300, 100, new string('d', 64), Now);
        brand.ChangeDraft(brand.Draft.With("Northwind Cargo", OperationsSitePreset.Harbour, "#0F4C5C", "#E36414", OperationsSiteServices.Air | OperationsSiteServices.Ocean, false, true,
            ("https://facebook.com/northwind", null, null, null, null, null)).WithImage(OperationsSiteImageKind.Logo, logo.Id), Now);
        brand.SetDraftText("es", "headline", "Envíe con Northwind", Now);
        brand.Publish(Now.AddMinutes(1));
        brand.SetDraftText("es", "headline", "Nuevo título", Now.AddMinutes(2));

        await Save(tenant, context =>
        {
            context.TenantBrands.Add(brand);
            context.SiteImages.Add(logo);
        });

        await using OperationsDbContext read = _database.NewContext(tenant);
        OperationsSiteRepository repository = new(read);
        OperationsTenantBrand found = (await repository.FindBrandAsync(CancellationToken.None))!;

        Assert.Equal("Northwind Cargo", found.DisplayName);
        Assert.Equal("#0F4C5C", found.Published.PrimaryColour);
        Assert.Equal(OperationsSiteServices.Air | OperationsSiteServices.Ocean, found.Published.Services);
        Assert.Equal("https://facebook.com/northwind", found.Published.FacebookUrl);
        Assert.Equal(logo.Id, found.Published.LogoImageId);
        Assert.Equal("Envíe con Northwind", found.Text(OperationsSiteTextStage.Published, "es", "headline"));
        Assert.Equal("Nuevo título", found.Text(OperationsSiteTextStage.Draft, "es", "headline"));
        Assert.True(found.HasUnpublishedChanges);
        Assert.Equal(new byte[] { 1, 2, 3 }, (await repository.GetImageAsync(logo.Id, CancellationToken.None))!.Content);
    }

    [SqlServerFact]
    public async Task Locations_StayInTheirTenant_AndCodesAreUniquePerTenant()
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        OperationsBranch branch = OperationsBranch.Create(first, Details("PAP", "HT"), OperationsBranchSide.Destination, Now);
        OperationsWarehouse warehouse = OperationsWarehouse.Create(first, branch.Id, Details("FLL", "US"), OperationsWarehouseRoles.Receiving | OperationsWarehouseRoles.PickupPoint, true, Now);

        await Save(first, context =>
        {
            context.Branches.Add(branch);
            context.Warehouses.Add(warehouse);
            context.CountryContacts.Add(OperationsCountryContact.Create(first, "HT", "Sales", new[] { "+50922223333", "+50922223334" }, "+50933334444", "info@northwind.example", "Mon-Sat", 1, Now));
        });
        await Save(second, context => context.Branches.Add(OperationsBranch.Create(second, Details("PAP", "HT"), OperationsBranchSide.Both, Now)));

        await using OperationsDbContext asFirst = _database.NewContext(first);
        OperationsLocationRepository locations = new(asFirst);
        OperationsWarehouse found = Assert.Single(await locations.ListWarehousesAsync(CancellationToken.None));
        Assert.Equal("US", found.Address.CountryCode);
        Assert.Equal(OperationsWarehouseRoles.Receiving | OperationsWarehouseRoles.PickupPoint, found.Roles);
        Assert.Single(await locations.ListBranchesAsync(CancellationToken.None));
        OperationsCountryContact contact = Assert.Single(await locations.ListContactsAsync(CancellationToken.None));
        Assert.Equal("+50933334444", contact.WhatsApp);
        Assert.Equal(new[] { "+50922223333", "+50922223334" }, contact.Phones);
        Assert.True(await locations.CodeTakenAsync("FLL", null, CancellationToken.None));
        Assert.False(await locations.CodeTakenAsync("FLL", warehouse.Id, CancellationToken.None));

        await using OperationsDbContext asSecond = _database.NewContext(second);
        Assert.Empty(await asSecond.Warehouses.ToListAsync());
        Assert.Empty(await asSecond.CountryContacts.ToListAsync());

        await Assert.ThrowsAsync<DbUpdateException>(() => Save(first, context => context.Branches.Add(OperationsBranch.Create(first, Details("PAP", "HT"), OperationsBranchSide.Both, Now))));
    }

    [Fact]
    public void ImageCheck_ReadsPngJpegAndWebp_ByTheirContent()
    {
        IOperationsImageInspector inspector = new OperationsImageInspector();

        OperationsImageInfo png = inspector.Inspect(Png(320, 120))!;
        Assert.Equal(("image/png", 320, 120), (png.ContentType, png.Width, png.Height));
        Assert.Equal(64, png.Sha256.Length);

        byte[] jpeg = { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x04, 0x00, 0x00, 0xFF, 0xC0, 0x00, 0x11, 0x08, 0x00, 0x64, 0x00, 0xC8, 0x03, 0x01, 0x22, 0x00 };
        OperationsImageInfo found = inspector.Inspect(jpeg)!;
        Assert.Equal(("image/jpeg", 200, 100), (found.ContentType, found.Width, found.Height));

        byte[] webp = new byte[30];
        "RIFF"u8.CopyTo(webp);
        "WEBPVP8X"u8.CopyTo(webp.AsSpan(8));
        webp[24] = 63;
        webp[27] = 31;
        OperationsImageInfo web = inspector.Inspect(webp)!;
        Assert.Equal(("image/webp", 64, 32), (web.ContentType, web.Width, web.Height));
    }

    [Theory]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\" onload=\"alert(1)\"></svg>")]
    [InlineData("GIF89a.........................")]
    [InlineData("<html><script>alert(1)</script></html>")]
    [InlineData("")]
    public void ImageCheck_RefusesEverythingElse(string content)
    {
        Assert.Null(new OperationsImageInspector().Inspect(System.Text.Encoding.UTF8.GetBytes(content)));
    }

    // A PNG header: signature, then the IHDR chunk with the size.
    private static byte[] Png(int width, int height)
    {
        byte[] data = new byte[33];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13, (byte)'I', (byte)'H', (byte)'D', (byte)'R' }.CopyTo(data, 0);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(16), width);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(20), height);
        return data;
    }

    private static OperationsLocationDetails Details(string code, string country) =>
        new(code, code + " office", OperationsPostalAddress.Create("1 Main Street", "Suite 2", "City", "Region", "33301", country), "+13055550100", "office@northwind.example", "Mon-Fri", true);

    private async Task Save(Guid tenant, Action<OperationsDbContext> change)
    {
        await using OperationsDbContext context = _database.NewContext(tenant);
        change(context);
        await context.SaveChangesAsync();
    }
}
