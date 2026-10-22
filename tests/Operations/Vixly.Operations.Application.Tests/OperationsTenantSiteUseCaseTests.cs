using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Site;
using Vixly.Operations.Application.Tests.Fakes;
using Vixly.Operations.Application.UseCases.Locations.SaveOperationsBranch;
using Vixly.Operations.Application.UseCases.Locations.SaveOperationsCountryContact;
using Vixly.Operations.Application.UseCases.Locations.SaveOperationsWarehouse;
using Vixly.Operations.Application.UseCases.Site.GetOperationsPublicSite;
using Vixly.Operations.Application.UseCases.Site.GetOperationsSiteImage;
using Vixly.Operations.Application.UseCases.Site.PublishOperationsSite;
using Vixly.Operations.Application.UseCases.Site.TrackOperationsShipmentPublicly;
using Vixly.Operations.Application.UseCases.Site.UpdateOperationsSiteDraft;
using Vixly.Operations.Application.UseCases.Site.UploadOperationsSiteImage;
using Vixly.Operations.Domain.Branding;
using Vixly.Operations.Domain.Identity;
using Vixly.Operations.Domain.Locations;

namespace Vixly.Operations.Application.Tests;

// =========================================================
// FILE: OperationsTenantSiteUseCaseTests.cs
//
// PURPOSE:
// Proves the Slice 1B use cases: public site, draft, images, publish, locations and tracking.
//
// RESPONSIBILITIES:
// - Visitors see the published version; the draft only by the signed-in Owner
// - Editing needs the signed-in Owner and is audited
// - Images are checked by their content
// - Locations are grouped by their own country
// - Public tracking answers not found
//
// IMPORTANT NOTES:
// - In-memory fakes
// =========================================================
public sealed class OperationsTenantSiteUseCaseTests
{
    private readonly IdentityTestKit _kit = new();

    private readonly FakeSiteStore _site = new();

    private readonly OperationsAuthorizationContext _owner;

    public OperationsTenantSiteUseCaseTests()
    {
        OperationsUser owner = _kit.Owner(withApp: true);
        _owner = _kit.Context.WithUser(owner.Id, Guid.NewGuid());
        _site.Brand = OperationsTenantBrand.Create(_kit.TenantId, "Northwind Freight", _kit.Clock.GetUtcNow());
    }

    [Fact]
    public async Task Draft_IsVisible_OnlyToTheOwnerPreview_UntilPublished()
    {
        Assert.True((await Update(headline: "Ship with us")).Succeeded);

        OperationsPublicSite visitor = (await PublicSite(_kit.Context, preview: true)).Site!;
        OperationsPublicSite ownerPreview = (await PublicSite(_owner, preview: true)).Site!;

        Assert.False(visitor.IsPreview);
        Assert.Null(visitor.Text("headline"));
        Assert.Equal("Northwind Freight", visitor.Settings.DisplayName);
        Assert.True(ownerPreview.IsPreview);
        Assert.Equal("Ship with us", ownerPreview.Text("headline"));
        Assert.Equal("Northwind Cargo", ownerPreview.Settings.DisplayName);

        Assert.True((await Publish(_owner)).Succeeded);

        OperationsPublicSite published = (await PublicSite(_kit.Context, preview: false)).Site!;
        Assert.Equal("Ship with us", published.Text("headline"));
        Assert.Equal("Northwind Cargo", published.Settings.DisplayName);
        Assert.Equal(1, published.Version);
        Assert.Contains(_kit.Store.Audit, entry => entry.ActionCode == "ops.site.published");
    }

    [Fact]
    public async Task Editing_NeedsTheSignedInOwner()
    {
        Assert.Equal("ops.access.sign_in_required", (await Update(context: _kit.Context)).Error?.Code);
        Assert.Equal("ops.access.sign_in_required", (await Publish(_kit.Context)).Error?.Code);
        Assert.Equal("ops.access.sign_in_required", (await Upload(_kit.Context)).Error?.Code);
        Assert.Equal("ops.access.sign_in_required", (await new SaveOperationsCountryContactUseCase(_site, Owner(), _kit.Audit, _kit.Store, _kit.Clock)
            .ExecuteAsync(_kit.Context, new SaveOperationsCountryContactRequest { CountryCode = "HT", Phones = new[] { "+50922223333" } }, CancellationToken.None)).Error?.Code);
    }

    [Fact]
    public async Task LowContrast_AndScript_AreRefused()
    {
        Assert.Equal("ops.brand.primary_contrast_too_low", (await Update(primary: "#FFEE00")).Error?.Code);
        Assert.Equal("ops.brand.text_not_plain", (await Update(headline: "<img src=x onerror=alert(1)>")).Error?.Code);
        Assert.False(_site.Brand!.HasUnpublishedChanges && _site.Brand.Draft.PrimaryColour == "#FFEE00");
    }

    [Fact]
    public async Task Image_IsCheckedByItsContent()
    {
        _site.Inspection = null;
        Assert.Equal("ops.brand.image_type_not_allowed", (await Upload(_owner)).Error?.Code);

        _site.Inspection = new OperationsImageInfo("image/png", 400, 100, new string('c', 64));
        UploadOperationsSiteImageResponse uploaded = await Upload(_owner);

        Assert.True(uploaded.Succeeded);
        OperationsSiteImage image = Assert.Single(_site.Images);
        Assert.Equal(image.Id, _site.Brand!.Draft.LogoImageId);
        Assert.Null(_site.Brand.Published.LogoImageId);

        GetOperationsSiteImageUseCase read = new(_site, Owner());
        Assert.False((await read.ExecuteAsync(_kit.Context, new GetOperationsSiteImageRequest { ImageId = image.Id }, CancellationToken.None)).Succeeded);
        GetOperationsSiteImageResponse draftImage = await read.ExecuteAsync(_owner, new GetOperationsSiteImageRequest { ImageId = image.Id }, CancellationToken.None);
        Assert.True(draftImage.Succeeded);
        Assert.False(draftImage.IsPublished);

        await Publish(_owner);
        GetOperationsSiteImageResponse publishedImage = await read.ExecuteAsync(_kit.Context, new GetOperationsSiteImageRequest { ImageId = image.Id }, CancellationToken.None);
        Assert.True(publishedImage.IsPublished);
        Assert.Equal("image/png", publishedImage.ContentType);
    }

    [Fact]
    public async Task Locations_AreListedUnderTheirOwnCountry_AndHiddenOnesStayHidden()
    {
        SaveOperationsBranchResponse haiti = await new SaveOperationsBranchUseCase(_site, Owner(), _kit.Audit, _kit.Store, _kit.Clock).ExecuteAsync(_owner, new SaveOperationsBranchRequest
        {
            Code = "PAP", Name = "Port-au-Prince", Line1 = "12 Rue Capois", City = "Port-au-Prince", CountryCode = "HT", ShowOnSite = true, Side = OperationsBranchSide.Destination
        }, CancellationToken.None);
        Assert.True(haiti.Succeeded);

        SaveOperationsWarehouseUseCase warehouses = new(_kit.Store, _site, Owner(), _kit.Audit, _kit.Store, _kit.Clock);
        Assert.True((await warehouses.ExecuteAsync(_owner, new SaveOperationsWarehouseRequest
        {
            BranchId = haiti.BranchId, Code = "FLL", Name = "Fort Lauderdale", Line1 = "1 Harbor Road", City = "Fort Lauderdale", Region = "FL", CountryCode = "US",
            Roles = OperationsWarehouseRoles.Receiving, AcceptingDeliveries = true, ShowOnSite = true
        }, CancellationToken.None)).Succeeded);
        Assert.True((await warehouses.ExecuteAsync(_owner, new SaveOperationsWarehouseRequest
        {
            BranchId = haiti.BranchId, Code = "SEC", Name = "Back office", Line1 = "2 Harbor Road", City = "Miami", CountryCode = "US",
            Roles = OperationsWarehouseRoles.DispatchHub | OperationsWarehouseRoles.Storage, ShowOnSite = false, TimeZone = "America/New_York"
        }, CancellationToken.None)).Succeeded);
        Assert.Equal("ops.destinations.time_zone_invalid", (await warehouses.ExecuteAsync(_owner, new SaveOperationsWarehouseRequest
        {
            BranchId = haiti.BranchId, Code = "TZ1", Name = "Elsewhere", Line1 = "4 Road", City = "Miami", CountryCode = "US", Roles = OperationsWarehouseRoles.Storage, TimeZone = "Miami"
        }, CancellationToken.None)).Error?.Code);
        Assert.Equal("ops.destinations.manager_invalid", (await warehouses.ExecuteAsync(_owner, new SaveOperationsWarehouseRequest
        {
            BranchId = haiti.BranchId, Code = "MG1", Name = "Elsewhere", Line1 = "4 Road", City = "Miami", CountryCode = "US", Roles = OperationsWarehouseRoles.Storage, ManagerUserId = Guid.NewGuid()
        }, CancellationToken.None)).Error?.Code);
        Assert.Equal("ops.locations.code_taken", (await warehouses.ExecuteAsync(_owner, new SaveOperationsWarehouseRequest
        {
            BranchId = haiti.BranchId, Code = "FLL", Name = "Again", Line1 = "3 Road", City = "Miami", CountryCode = "US", Roles = OperationsWarehouseRoles.Receiving
        }, CancellationToken.None)).Error?.Code);

        // Slice 4C: warehouses as destinations: Les Cayes, a hub and pickup point delivering home in two communes; Jérémie, run by a partner, served through Les Cayes.
        SaveOperationsWarehouseRequest cayes = new()
        {
            BranchId = haiti.BranchId, Code = "CAY", Name = "Les Cayes", Line1 = "1 Rue", City = "Les Cayes", CountryCode = "HT",
            Roles = OperationsWarehouseRoles.DispatchHub | OperationsWarehouseRoles.PickupPoint, DeliversHome = true, HomeArea = new[] { "Les Cayes", "Torbeck" }
        };
        SaveOperationsWarehouseResponse hub = await warehouses.ExecuteAsync(_owner, cayes, CancellationToken.None);
        Assert.True(hub.Succeeded);
        SaveOperationsWarehouseRequest jeremie = new()
        {
            BranchId = haiti.BranchId, Code = "JER", Name = "Jérémie", Line1 = "2 Rue", City = "Jérémie", CountryCode = "HT", Roles = OperationsWarehouseRoles.PickupPoint,
            HubWarehouseId = hub.WarehouseId, OperatedBy = OperationsDestinationOperator.Partner, PartnerName = "Grand'Anse Express", PartnerPhone = "+50937000000"
        };
        Assert.True((await warehouses.ExecuteAsync(_owner, jeremie, CancellationToken.None)).Succeeded);
        IReadOnlyList<OperationsWarehouse> all = await _site.ListWarehousesAsync(CancellationToken.None);
        OperationsWarehouse saved = all.Single(warehouse => warehouse.Code == "JER");
        Assert.Equal((true, OperationsDestinationOperator.Partner, "Grand'Anse Express", hub.WarehouseId), (saved.IsDestination, saved.OperatedBy, saved.PartnerName, saved.HubWarehouseId));
        OperationsWarehouse cayesSaved = all.Single(warehouse => warehouse.Code == "CAY");
        Assert.True(cayesSaved.Covers("TORBECK") && !cayesSaved.Covers("Jérémie") && cayesSaved.Offers(Vixly.Operations.Domain.Settings.OperationsDeliveryModes.HomeDelivery));
        Assert.False(all.Single(warehouse => warehouse.Code == "FLL").IsDestination);

        // A hub in another country or without the hub role, and a partner without a name, are refused.
        Assert.Equal("ops.destinations.hub_invalid", (await warehouses.ExecuteAsync(_owner, new SaveOperationsWarehouseRequest
        {
            BranchId = haiti.BranchId, Code = "MIR", Name = "Miragoâne", Line1 = "3 Rue", City = "Miragoâne", CountryCode = "HT", Roles = OperationsWarehouseRoles.PickupPoint,
            HubWarehouseId = all.Single(warehouse => warehouse.Code == "FLL").Id
        }, CancellationToken.None)).Error?.Code);
        Assert.Equal("ops.destinations.partner_invalid", (await warehouses.ExecuteAsync(_owner, new SaveOperationsWarehouseRequest
        {
            BranchId = haiti.BranchId, Code = "MIR", Name = "Miragoâne", Line1 = "3 Rue", City = "Miragoâne", CountryCode = "HT", Roles = OperationsWarehouseRoles.PickupPoint,
            OperatedBy = OperationsDestinationOperator.Partner
        }, CancellationToken.None)).Error?.Code);

        // Never a circle of hubs: Jacmel goes through Les Cayes, so Les Cayes cannot go through Jacmel.
        SaveOperationsWarehouseResponse jacmel = await warehouses.ExecuteAsync(_owner, new SaveOperationsWarehouseRequest
        {
            BranchId = haiti.BranchId, Code = "JAK", Name = "Jacmel", Line1 = "5 Rue", City = "Jacmel", CountryCode = "HT", Roles = OperationsWarehouseRoles.DispatchHub,
            HubWarehouseId = hub.WarehouseId
        }, CancellationToken.None);
        Assert.True(jacmel.Succeeded);
        Assert.Equal("ops.destinations.hub_invalid", (await warehouses.ExecuteAsync(_owner, new SaveOperationsWarehouseRequest
        {
            WarehouseId = hub.WarehouseId, BranchId = haiti.BranchId, Code = "CAY", Name = "Les Cayes", Line1 = "1 Rue", City = "Les Cayes", CountryCode = "HT",
            Roles = OperationsWarehouseRoles.DispatchHub | OperationsWarehouseRoles.PickupPoint, HubWarehouseId = jacmel.WarehouseId
        }, CancellationToken.None)).Error?.Code);

        OperationsPublicSite site = (await PublicSite(_kit.Context, preview: false)).Site!;

        OperationsPublicLocation florida = Assert.Single(site.Locations, location => location.Name == "Fort Lauderdale");
        Assert.Equal("US", florida.CountryCode);
        Assert.True(florida.Receiving);
        Assert.Equal("HT", Assert.Single(site.Locations, location => location.Name == "Port-au-Prince").CountryCode);
        Assert.DoesNotContain(site.Locations, location => location.Name == "Back office");
        Assert.Equal(new[] { "HT", "US" }, site.Countries);
    }

    [Fact]
    public async Task PlanWithoutBranding_ShowsTheDefaults_AndKeepsTheChoices()
    {
        Assert.True((await Update(headline: "Ship with us", primary: "#0F4C5C")).Succeeded);
        Assert.True((await Publish(_owner)).Succeeded);
        Features.Tenants[_kit.TenantId] = new OperationsPlanFeatures(false, false);

        OperationsPublicSite limited = (await PublicSite(_kit.Context, preview: false)).Site!;

        Assert.Equal("#1D4ED8", limited.Settings.PrimaryColour);
        Assert.Null(limited.Text("headline"));
        Assert.Equal("Northwind Cargo", limited.Settings.DisplayName);
        Assert.Equal("#0F4C5C", _site.Brand!.Published.PrimaryColour);

        Features.Tenants[_kit.TenantId] = new OperationsPlanFeatures(true, false);
        OperationsPublicSite themed = (await PublicSite(_kit.Context, preview: false)).Site!;
        Assert.Equal("#0F4C5C", themed.Settings.PrimaryColour);
        Assert.Null(themed.Text("headline"));

        Features.Tenants[_kit.TenantId] = OperationsPlanFeatures.All;
        Assert.Equal("Ship with us", (await PublicSite(_kit.Context, preview: false)).Site!.Text("headline"));
    }

    [Fact]
    public async Task PublicTracking_AnswersNotFound_UntilTrackingArrives()
    {
        TrackOperationsShipmentPubliclyResponse response = await new TrackOperationsShipmentPubliclyUseCase().ExecuteAsync(
            _kit.Context, new TrackOperationsShipmentPubliclyRequest { Number = "VX123" }, CancellationToken.None);

        Assert.False(response.Succeeded);
        Assert.Empty(response.Milestones);
    }

    private FakePlanFeatures Features { get; } = new();

    private OperationsOwnerCheck Owner() => new(_kit.Store);

    private Task<GetOperationsPublicSiteResponse> PublicSite(OperationsAuthorizationContext context, bool preview) =>
        new GetOperationsPublicSiteUseCase(_site, _site, Owner(), Features).ExecuteAsync(context, new GetOperationsPublicSiteRequest { Language = "en", Preview = preview }, CancellationToken.None);

    private Task<UpdateOperationsSiteDraftResponse> Update(string? headline = null, string primary = "#1D4ED8", OperationsAuthorizationContext? context = null) =>
        new UpdateOperationsSiteDraftUseCase(_site, Owner(), _kit.Audit, _kit.Store, _kit.Clock).ExecuteAsync(context ?? _owner, new UpdateOperationsSiteDraftRequest
        {
            DisplayName = "Northwind Cargo",
            PrimaryColour = primary,
            AccentColour = "#0E7490",
            Services = OperationsSiteServices.All,
            ShowSteps = true,
            Language = "en",
            Texts = new Dictionary<string, string?> { ["headline"] = headline }
        }, CancellationToken.None);

    private Task<PublishOperationsSiteResponse> Publish(OperationsAuthorizationContext context) =>
        new PublishOperationsSiteUseCase(_site, Owner(), _kit.Audit, _kit.Store, _kit.Clock).ExecuteAsync(context, new PublishOperationsSiteRequest(), CancellationToken.None);

    private Task<UploadOperationsSiteImageResponse> Upload(OperationsAuthorizationContext context) =>
        new UploadOperationsSiteImageUseCase(_site, _site, Owner(), _kit.Audit, _kit.Store, _kit.Clock).ExecuteAsync(context, new UploadOperationsSiteImageRequest
        {
            Kind = OperationsSiteImageKind.Logo,
            Content = new byte[] { 0x89, 0x50, 0x4E, 0x47 }
        }, CancellationToken.None);
}
