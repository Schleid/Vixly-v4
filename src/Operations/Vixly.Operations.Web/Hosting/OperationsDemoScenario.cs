using System.Buffers.Binary;
using System.IO.Compression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Application.UseCases.Charges.SaveOperationsChargeExtra;
using Vixly.Operations.Application.UseCases.Charges.SaveOperationsInsurancePlan;
using Vixly.Operations.Application.UseCases.Charges.SaveOperationsPriceList;
using Vixly.Operations.Application.UseCases.Charges.SaveOperationsQuote;
using Vixly.Operations.Application.UseCases.Holds.ChangeOperationsStorageFee;
using Vixly.Operations.Application.UseCases.Holds.StartOperationsStockCount;
using Vixly.Operations.Application.UseCases.Insurance.LinkOperationsCoverPolicy;
using Vixly.Operations.Application.UseCases.Insurance.SaveOperationsInsurancePolicy;
using Vixly.Operations.Application.UseCases.Insurance.SaveOperationsLiabilityTerms;
using Vixly.Operations.Application.UseCases.Locations.SaveOperationsBranch;
using Vixly.Operations.Application.UseCases.Locations.SaveOperationsWarehouse;
using Vixly.Operations.Application.UseCases.Printing.GetOperationsShippingLabel;
using Vixly.Operations.Application.UseCases.Receiving.AssignOperationsParcel;
using Vixly.Operations.Application.UseCases.Receiving.CheckOperationsParcel;
using Vixly.Operations.Application.UseCases.Receiving.CloseOperationsDelivery;
using Vixly.Operations.Application.UseCases.Receiving.CreateMyOperationsPreAlert;
using Vixly.Operations.Application.UseCases.Receiving.OpenOperationsDelivery;
using Vixly.Operations.Application.UseCases.Receiving.PutAwayOperationsParcel;
using Vixly.Operations.Application.UseCases.Receiving.RecordOperationsParcelLabel;
using Vixly.Operations.Application.UseCases.Receiving.SaveOperationsStorageLocation;
using Vixly.Operations.Application.UseCases.Receiving.ScanOperationsParcel;
using Vixly.Operations.Application.UseCases.Settings.SaveOperationsShippingServices;
using Vixly.Operations.Application.UseCases.Settings.UpdateOperationsTenantSettings;
using Vixly.Operations.Application.UseCases.Shipments.CreateMyOperationsShipment;
using Vixly.Operations.Application.UseCases.Supplies.SaveOperationsSupply;
using Vixly.Operations.Application.UseCases.Tickets.CreateMyOperationsTicket;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Insurance;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Supplies;
using Vixly.Operations.Domain.Tickets;
using Vixly.Operations.Infrastructure.Persistence;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Web.Hosting;

// =========================================================
// FILE: OperationsDemoScenario.cs
//
// PURPOSE:
// In Development only, fills two demo companies with a working day of sample data, so the owner can see what the rules decide
// (owner, 2026-10-19: "mock data ... helps me better understand your logic against mine").
//
// RESPONSIBILITIES:
// - Set up a company: settings, origin and destinations, shelves, services, prices, an extra fee, insurance, storage fee
// - Act as its customers (pre-alerts, shipments, a ticket) and its staff (dock scans, inspection, labels, shelves, undeclared parcels)
// - Leave every stage visible: expected, overdue, received, on hold, cleared, labelled, undeclared, unclaimed, counted
//
// IMPORTANT NOTES:
// - Everything goes through the real use cases, signed in as the demo Administrator and the demo customers, so the data is what the
//   rules decide, with its tracking history; only dates are moved back afterwards (a package 40 days old cannot arrive today)
// - Runs once per company: a company that already has the MIA warehouse is left alone
// - Fictional people and companies; emails use .example; the story is told in docs/operations/DEVELOPMENT-DEMO-DATA.md
// =========================================================
public sealed class OperationsDemoScenario
{
    private readonly IServiceScopeFactory _scopes;

    private readonly OperationsDemoDataOptions _options;

    private readonly TimeProvider _timeProvider;

    private Guid _tenant;

    private string _slug = string.Empty;

    private OperationsAuthorizationContext _staff = OperationsAuthorizationContext.Host;

    public OperationsDemoScenario(IServiceScopeFactory scopes, IOptions<OperationsDemoDataOptions> options, TimeProvider timeProvider)
    {
        _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    // The main company (Miami to Haiti, the Dominican Republic and Peru); the second only proves one company never sees another's data.
    public async Task<bool> SeedAsync(Guid tenantId, string slug, bool main, CancellationToken cancellationToken)
    {
        (_tenant, _slug) = (tenantId, slug);

        if (await QueryAsync(context => context.Warehouses.AnyAsync(item => item.Code == "MIA", cancellationToken)))
        {
            return false;
        }

        Guid admin = await QueryAsync(async context =>
            (await context.Users.SingleAsync(user => user.Email == EmailAddress.Create("admin@" + slug + ".example"), cancellationToken)).Id);
        _staff = OperationsAuthorizationContext.ForTenant(tenantId).WithUser(admin, Guid.NewGuid());

        if (main)
        {
            await MainAsync(cancellationToken);
        }
        else
        {
            await SecondAsync(cancellationToken);
        }

        return true;
    }

    // Slice 5E: a demo company seeded before a slice gets that slice's sample data once (only what it is still missing).
    public async Task<bool> TopUpAsync(Guid tenantId, string slug, bool main, CancellationToken cancellationToken)
    {
        (_tenant, _slug) = (tenantId, slug);

        if (!main || await QueryAsync(context => context.Supplies.AnyAsync(cancellationToken))
            || !await QueryAsync(context => context.Warehouses.AnyAsync(item => item.Code == "MIA", cancellationToken)))
        {
            return false;
        }

        Guid admin = await QueryAsync(async context =>
            (await context.Users.SingleAsync(user => user.Email == EmailAddress.Create("admin@" + slug + ".example"), cancellationToken)).Id);
        _staff = OperationsAuthorizationContext.ForTenant(tenantId).WithUser(admin, Guid.NewGuid());
        await SuppliesAsync(cancellationToken);
        return true;
    }

    // =====================================================
    // BLUEWATER SHIPPING: Miami to Haiti, the Dominican Republic and Peru
    // =====================================================
    private async Task MainAsync(CancellationToken ct)
    {
        await SettingsAsync("USD", OperationsWeightUnit.Pound, OperationsDimensionUnit.Inch, "America/New_York", ct);
        Guid miami = await BranchAsync("MIAMI", "Miami", "8400 NW 25th St", "Doral", "FL", "33122", "US", OperationsBranchSide.Origin, ct);
        Guid haiti = await BranchAsync("PAPOFF", "Port-au-Prince", "12 Rue Capois", "Port-au-Prince", null, null, "HT", OperationsBranchSide.Destination, ct);
        Guid cayes = await BranchAsync("CAYOFF", "Les Cayes", "45 Rue Geffrard", "Les Cayes", null, null, "HT", OperationsBranchSide.Destination, ct);
        Guid jeremie = await BranchAsync("JEROFF", "Jérémie", "8 Rue Stenio Vincent", "Jérémie", null, null, "HT", OperationsBranchSide.Destination, ct);
        Guid dominican = await BranchAsync("SDQOFF", "Santo Domingo", "Av. 27 de Febrero 210", "Santo Domingo", null, null, "DO", OperationsBranchSide.Destination, ct);
        Guid peru = await BranchAsync("LIMOFF", "Lima", "Av. Larco 1150", "Miraflores", "Lima", null, "PE", OperationsBranchSide.Destination, ct);

        Guid mia = await WarehouseAsync(miami, "MIA", "Doral receiving", "8400 NW 25th St", "Doral", "FL", "33122", "US",
            OperationsWarehouseRoles.Receiving | OperationsWarehouseRoles.Storage, false, null, null, "America/New_York", ct);
        Guid pap = await WarehouseAsync(haiti, "PAP", "Port-au-Prince hub", "12 Rue Capois", "Port-au-Prince", null, null, "HT",
            OperationsWarehouseRoles.PickupPoint | OperationsWarehouseRoles.DispatchHub, true, new[] { "Port-au-Prince", "Pétion-Ville", "Delmas" }, null, "America/Port-au-Prince", ct);
        Guid cay = await WarehouseAsync(cayes, "CAY", "Les Cayes office", "45 Rue Geffrard", "Les Cayes", null, null, "HT",
            OperationsWarehouseRoles.PickupPoint, true, new[] { "Les Cayes", "Torbeck" }, pap, "America/Port-au-Prince", ct);
        Guid jer = await WarehouseAsync(jeremie, "JER", "Jérémie office", "8 Rue Stenio Vincent", "Jérémie", null, null, "HT",
            OperationsWarehouseRoles.PickupPoint, false, null, pap, "America/Port-au-Prince", ct);
        Guid sdq = await WarehouseAsync(dominican, "SDQ", "Santo Domingo office", "Av. 27 de Febrero 210", "Santo Domingo", null, null, "DO",
            OperationsWarehouseRoles.PickupPoint, true, new[] { "Santo Domingo" }, null, "America/Santo_Domingo", ct);
        Guid lim = await WarehouseAsync(peru, "LIM", "Lima partner", "Av. Larco 1150", "Miraflores", "Lima", null, "PE",
            OperationsWarehouseRoles.PickupPoint, true, new[] { "Lima", "Miraflores" }, null, "America/Lima", ct, partner: "Rímac Entregas");

        Dictionary<string, Guid> shelves = new(StringComparer.Ordinal);

        foreach ((string code, OperationsStorageZone zone) in new[]
        {
            ("A-01-1", OperationsStorageZone.Storage), ("A-01-2", OperationsStorageZone.Storage), ("B-01-1", OperationsStorageZone.Storage),
            ("H-01", OperationsStorageZone.Hold), ("U-01", OperationsStorageZone.Undeclared), ("U-02", OperationsStorageZone.Undeclared)
        })
        {
            shelves[code] = await ShelfAsync(mia, code, zone, ct);
        }

        await ServicesAsync(new[] { "air_standard", "air_express", "ocean_consolidated" }, OperationsWeightUnit.Pound, OperationsDimensionUnit.Inch, ct);

        // Prices: the flat bands the owner described (0 to 10 lb, 10 to 20 lb, then per lb), dearer to the provinces, and one customer's own card.
        await PriceAsync("air_standard", mia, null, null, Bands((0, OperationsBandKind.Flat, 50m), (10, OperationsBandKind.Flat, 65m), (20, OperationsBandKind.PerUnit, 3.25m)), 50m, ct);
        await PriceAsync("air_standard", mia, cay, null, Bands((0, OperationsBandKind.Flat, 55m), (10, OperationsBandKind.Flat, 72m), (20, OperationsBandKind.PerUnit, 3.60m)), 55m, ct);
        await PriceAsync("air_standard", mia, jer, null, Bands((0, OperationsBandKind.Flat, 58m), (10, OperationsBandKind.Flat, 75m), (20, OperationsBandKind.PerUnit, 3.75m)), 58m, ct);
        await PriceAsync("air_express", mia, null, null, Bands((0, OperationsBandKind.PerUnit, 6.50m)), 35m, ct);
        await PriceAsync("ocean_consolidated", mia, null, null, Bands((0, OperationsBandKind.PerUnit, 1.40m)), 40m, ct);
        await ExtraAsync("CONATEL fee", 10m, "iphone, ipad, samsung galaxy", "HT", ct);
        // Slice 5D: two air products the clerk can offer side by side, and one for ocean.
        await InsuranceAsync(OperationsTransportMode.Air, "Basic", 0, 3m, 5m, 5000m, "Loss, damage and theft up to 5,000 USD per shipment.", ct);
        await InsuranceAsync(OperationsTransportMode.Air, "Full value", 1, 4.5m, 10m, 20000m, "Full declared value up to 20,000 USD, no deductible. For electronics and valuables.", ct);
        await InsuranceAsync(OperationsTransportMode.Ocean, "Ocean cargo", 0, 2m, 10m, 10000m, null, ct);
        await SuppliesAsync(ct);
        Guid basicAir = await WithAsync(async services => (await services.GetRequiredService<IOperationsChargeRepository>().ListInsurancePlansAsync(ct))
            .First(plan => plan.Mode == OperationsTransportMode.Air && plan.Name == "Basic").Id);

        // Slice 5B: the company's two kinds of insurance (I-6): an open cargo policy behind the cover it sells, and a freight forwarder's liability
        // policy for what its own team or warehouses damage; and what it owes when a customer does not insure.
        DateOnly start = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime).AddMonths(-10);
        Guid cargo = await PolicyAsync(new OperationsPolicyTerms(OperationsPolicyScope.CustomerShipments, OperationsPolicyKind.OpenCargo, "Atlantic Marine Insurance Co.",
            "service@atlantic-marine.example", "+13055550190", "claims@atlantic-marine.example", "+13055550191", "Harbor Brokers", "desk@harbor-brokers.example", "+13055550192",
            "OC-2026-11873", start, start.AddYears(1).AddDays(-1), "USD", 25000m, 250000m, null, 1000000m, 250m, null, null,
            OperationsPolicyRisks.Loss | OperationsPolicyRisks.Damage | OperationsPolicyRisks.Theft | OperationsPolicyRisks.Wetting, OperationsPolicyModes.Air | OperationsPolicyModes.Ocean,
            "United States to the Caribbean and Latin America", Vixly.Operations.Domain.Charges.OperationsInsuranceExclusions.Standard, null, 30, OperationsDeclarationFrequency.Monthly,
            "Monthly declaration due by the 10th."), ct);
        await PolicyAsync(new OperationsPolicyTerms(OperationsPolicyScope.CompanyOperations, OperationsPolicyKind.FreightForwarderLiability, "Gulfstream Mutual",
            null, "+13055550193", "claims@gulfstream-mutual.example", null, "Harbor Brokers", null, null, "FFL-55-0921", start.AddMonths(-1), start.AddMonths(10).AddDays(20), "USD",
            100000m, null, 500000m, 2000000m, 1000m, null, null, OperationsPolicyRisks.Loss | OperationsPolicyRisks.Damage | OperationsPolicyRisks.Theft, OperationsPolicyModes.None,
            null, Vixly.Operations.Domain.Charges.OperationsInsuranceExclusions.None, null, 14, OperationsDeclarationFrequency.None, "Covers our warehouses, hubs, vehicles and team."), ct);

        foreach (Vixly.Operations.Domain.Charges.OperationsInsurancePlan product in await WithAsync(async services =>
            await services.GetRequiredService<IOperationsChargeRepository>().ListInsurancePlansAsync(ct)))
        {
            await Use<LinkOperationsCoverPolicyUseCase>(async useCase => Ok(await useCase.ExecuteAsync(_staff,
                new LinkOperationsCoverPolicyRequest { Mode = product.Mode, PlanId = product.Id, PolicyId = cargo }, ct), "cover policy"));
        }

        await Use<SaveOperationsLiabilityTermsUseCase>(async useCase => Ok(await useCase.ExecuteAsync(_staff, new SaveOperationsLiabilityTermsRequest
        {
            Mode = OperationsTransportMode.Air, Basis = OperationsLiabilityBasis.PerWeight, Amount = 9.07m, Cap = 100m,
            CustomerText = "Not insured: we pay up to $9.07 per lb lost or damaged, at most $100 per parcel."
        }, ct), "liability air"));
        await Use<SaveOperationsLiabilityTermsUseCase>(async useCase => Ok(await useCase.ExecuteAsync(_staff, new SaveOperationsLiabilityTermsRequest
        {
            Mode = OperationsTransportMode.Ocean, Basis = OperationsLiabilityBasis.PerParcel, Amount = 50m
        }, ct), "liability ocean"));
        await Use<ChangeOperationsStorageFeeUseCase>(async useCase => Ok(await useCase.ExecuteAsync(_staff,
            new ChangeOperationsStorageFeeRequest { FreeDays = 10, FeePerDay = 0.50m }, ct), "storage fee"));

        // Customers (fictional), each with a reason to be here.
        Guid ana = await CustomerAsync("Ana", "Pierre", null, "ana", LanguageCode.Fr, ct);
        Guid marc = await CustomerAsync("Marc", "Dubois", "Dubois Cartons", "marc", LanguageCode.Fr, ct);
        Guid lucie = await CustomerAsync("Lucie", "Jean", null, "lucie", LanguageCode.En, ct);
        Guid noel = await CustomerAsync("Jean-Baptiste", "Noël", null, "jb", LanguageCode.Fr, ct);
        string anaSession = await SessionAsync(ana, ct);
        string marcSession = await SessionAsync(marc, ct);
        string lucieSession = await SessionAsync(lucie, ct);
        string noelSession = await SessionAsync(noel, ct);
        string marcCode = await CodeAsync(marc, ct);
        string lucieCode = await CodeAsync(lucie, ct);
        DateOnly today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);

        // Ana: one shipment to her home in Les Cayes by air; four packages announced, at four different stages.
        Guid anaShoes = await PreAlertAsync(anaSession, mia, "Amazon", "TBA301884422190", "Amazon", "Two pairs of running shoes", 89.90m, today.AddDays(-2), ct);
        Guid anaDresses = await PreAlertAsync(anaSession, mia, "UPS", "1Z999AA10123456701", "Shein", "Three summer dresses", 64.50m, today.AddDays(-1), ct);
        Guid anaVitamins = await PreAlertAsync(anaSession, mia, "USPS", "9400111899223344556677", "Walmart", "Vitamins and a blood pressure monitor", 72.00m, today.AddDays(3), ct);
        Guid anaCase = await PreAlertAsync(anaSession, mia, "FedEx", "771234567890", "eBay", "Phone case and charger", 25.00m, today.AddDays(-6), ct);
        await ShipmentAsync(anaSession, mia, Home("45 Rue Geffrard", "Les Cayes", "HT", "Ana Pierre", "+50937112233", cay, "air_standard", insured: true) with { InsurancePlanId = basicAir },
            new[] { anaShoes, anaDresses, anaVitamins, anaCase }, ct);

        // Marc (Dubois Cartons): cartons he drops off himself to the Dominican Republic by sea (no pre-alert, the counter comes in Slice 5),
        // and Amazon tape for a customer in Peru, pre-alerted and put in a second shipment (owner, 2026-10-19).
        await ShipmentAsync(marcSession, mia, Pickup("Rafael Méndez", "+18095550144", sdq, "DO", "ocean_consolidated"), Array.Empty<Guid>(), ct);
        Guid marcTape = await PreAlertAsync(marcSession, mia, "Amazon", "TBA302775533110", "Amazon", "Packing tape, 36 rolls", 54.00m, today.AddDays(-1), ct);
        await ShipmentAsync(marcSession, mia, Home("Calle Alcanfores 455", "Miraflores", "PE", "Carlos Rojas", "+51987654321", lim, "air_standard", insured: false),
            new[] { marcTape }, ct);

        // Lucie: one package announced but put in no shipment; another she never announced.
        await PreAlertAsync(lucieSession, mia, "UPS", "1Z999AA10123456702", "Target", "Bed sheets", 45.00m, today.AddDays(-1), ct);

        // Jean-Baptiste: an iPhone to the Jérémie office (the CONATEL fee applies), picked for a compliance inspection.
        Guid noelPhone = await PreAlertAsync(noelSession, mia, "FedEx", "771234567891", "Apple", "iPhone 16, new in box", 899.00m, today.AddDays(-1), ct);
        await ShipmentAsync(noelSession, mia, Pickup("Jean-Baptiste Noël", "+50938445566", jer, "HT", "air_express"), new[] { noelPhone }, ct);

        // Today at the dock: three carrier deliveries; one scan per package, one ID label each.
        (Guid amazon, Dictionary<string, Guid> fromAmazon) = await DeliveryAsync(mia, "Amazon", new[] { "TBA301884422190", "TBA302775533110", "TBA309911002244" }, ct);
        (Guid ups, Dictionary<string, Guid> fromUps) = await DeliveryAsync(mia, "UPS", new[] { "1Z999AA10123456701", "1Z999AA10123456702", "1Z999AA10123454421" }, ct);
        (Guid fedex, Dictionary<string, Guid> fromFedex) = await DeliveryAsync(mia, "FedEx", new[] { "771234567891", "783300112233", "790011223344" }, ct);

        // The inspection station: condition, weight, size, photos.
        await CheckAsync(fromAmazon["TBA301884422190"], "intact", 4.2m, 14m, 10m, 6m, ct);
        await CheckAsync(fromUps["1Z999AA10123456701"], "intact", 2.6m, 12m, 9m, 4m, ct);
        await CheckAsync(fromAmazon["TBA302775533110"], "damaged", 11.8m, 16m, 12m, 10m, ct);
        await CheckAsync(fromUps["1Z999AA10123456702"], "intact", 3.1m, 15m, 12m, 3m, ct);
        await CheckAsync(fromFedex["771234567891"], "intact", 1.4m, 8m, 5m, 3m, ct, flag: "New phone: check the CONATEL declaration");
        await CheckAsync(fromFedex["783300112233"], "intact", 6.0m, 18m, 14m, 8m, ct);
        await CheckAsync(fromUps["1Z999AA10123454421"], "intact", 2.2m, 13m, 9m, 5m, ct);
        await CheckAsync(fromAmazon["TBA309911002244"], "intact", 1.1m, 9m, 6m, 2m, ct);

        // Ana's shoes: cleared, the shipping label printed and stuck on, then shelved. Her dresses: cleared, label still to print.
        await Use<GetOperationsShippingLabelUseCase>(async useCase => Ok(await useCase.ExecuteAsync(_staff,
            new GetOperationsShippingLabelRequest { ParcelId = fromAmazon["TBA301884422190"] }, ct), "shipping label"));
        await ShelveAsync(fromAmazon["TBA301884422190"], shelves["A-01-1"], ct);
        await ShelveAsync(fromAmazon["TBA302775533110"], shelves["H-01"], ct);

        // Lucie's Best Buy box, never announced: nobody can be sure it is hers until she says so, so staff find her by the name and code
        // on the label, assign it (it joins her waiting shipment, held for her information).
        await Use<RecordOperationsParcelLabelUseCase>(async useCase => Ok(await useCase.ExecuteAsync(_staff,
            new RecordOperationsParcelLabelRequest { ParcelId = fromFedex["783300112233"], Name = "Lucie Jean", Code = lucieCode, Sender = "Best Buy" }, ct), "label text"));
        await Use<AssignOperationsParcelUseCase>(async useCase => Ok(await useCase.ExecuteAsync(_staff, new AssignOperationsParcelRequest
        {
            ParcelId = fromFedex["783300112233"], CustomerCode = lucieCode, VerifiedBy = "label_code", CommandKey = Key(), PortalSlug = _slug
        }, ct), "assign"));

        // The undeclared area: a UPS package with a name only (it resembles Ana's ticket below), an Amazon one with nothing readable,
        // and one from a FedEx delivery 40 days ago nobody claimed (the company decides: return, dispose of or auction).
        await Use<RecordOperationsParcelLabelUseCase>(async useCase => Ok(await useCase.ExecuteAsync(_staff,
            new RecordOperationsParcelLabelRequest { ParcelId = fromUps["1Z999AA10123454421"], Name = "A. Pierre", Sender = "Nike" }, ct), "label text"));
        await ShelveAsync(fromUps["1Z999AA10123454421"], shelves["U-01"], ct);
        await ShelveAsync(fromAmazon["TBA309911002244"], shelves["U-01"], ct);
        (Guid old, Dictionary<string, Guid> fromOld) = await DeliveryAsync(mia, "FedEx", new[] { "789900112277" }, ct);
        await CheckAsync(fromOld["789900112277"], "intact", 7.5m, 20m, 14m, 9m, ct);
        await ShelveAsync(fromOld["789900112277"], shelves["U-02"], ct);

        foreach (Guid delivery in new[] { amazon, ups, fedex, old })
        {
            await Use<CloseOperationsDeliveryUseCase>(async useCase => Ok(await useCase.ExecuteAsync(_staff, new CloseOperationsDeliveryRequest { DeliveryId = delivery }, ct), "close delivery"));
        }

        // Ana opens a ticket: Nike says the shoes were delivered, they are not in her account (the undeclared UPS package resembles it).
        await Use<CreateMyOperationsTicketUseCase>(async useCase => Ok(await useCase.ExecuteAsync(OperationsAuthorizationContext.ForTenant(_tenant), new CreateMyOperationsTicketRequest
        {
            SessionToken = anaSession,
            WarehouseId = mia,
            Details = new OperationsTicketDetails("UPS", "1ZW45E670323454421", "Nike", "C01-77881", today.AddDays(-2), "Nike Air running shoes, size 8"),
            Message = "UPS shows it delivered to your warehouse two days ago, signed by the front desk.",
            Proof = Png(0x2B, 0x6C, 0xB0)
        }, ct), "ticket"));

        // The monthly count of the undeclared area, started this morning.
        await Use<StartOperationsStockCountUseCase>(async useCase => Ok(await useCase.ExecuteAsync(_staff, new StartOperationsStockCountRequest { WarehouseId = mia }, ct), "stock count"));

        // A quote for Dubois Cartons, with a preferred price for a valued customer.
        await Use<SaveOperationsQuoteUseCase>(async useCase => Ok(await useCase.ExecuteAsync(_staff, new SaveOperationsQuoteRequest
        {
            CustomerCode = marcCode,
            OriginWarehouseId = mia,
            DestinationWarehouseId = sdq,
            ServiceCode = "air_standard",
            Pieces = Enumerable.Range(0, 4).Select(_ => new Vixly.Operations.Application.Charges.OperationsPieceInput(250m, 40m, 30m, 30m, 600m, null, "Corrugated cartons, 250 per pack")).ToList(),
            PreferredTotal = 2900m,
            Reason = "Valued customer: four packs a month",
            CommandKey = Key()
        }, ct), "quote"));

        // Dates a single day cannot give: the unclaimed package arrived 40 days ago; the overdue pre-alerts stay overdue.
        await QueryAsync(async context =>
        {
            DateTimeOffset longAgo = _timeProvider.GetUtcNow().AddDays(-40);
            Guid oldParcel = fromOld["789900112277"];
            await context.Parcels.Where(item => item.Id == oldParcel).ExecuteUpdateAsync(setters => setters.SetProperty(item => item.CreatedUtc, longAgo), ct);
            await context.InboundDeliveries.Where(item => item.Id == old).ExecuteUpdateAsync(setters => setters.SetProperty(item => item.CreatedUtc, longAgo), ct);
            return true;
        });
    }

    // =====================================================
    // ANDES COURIER: a second company, for isolation checks
    // =====================================================
    private async Task SecondAsync(CancellationToken ct)
    {
        await SettingsAsync("USD", OperationsWeightUnit.Kilogram, OperationsDimensionUnit.Centimetre, "America/Lima", ct);
        Guid miami = await BranchAsync("MIAMI", "Miami", "2200 NW 72nd Ave", "Miami", "FL", "33122", "US", OperationsBranchSide.Origin, ct);
        Guid lima = await BranchAsync("LIMOFF", "Lima", "Av. Arequipa 3500", "San Isidro", "Lima", null, "PE", OperationsBranchSide.Destination, ct);
        Guid mia = await WarehouseAsync(miami, "MIA", "Miami receiving", "2200 NW 72nd Ave", "Miami", "FL", "33122", "US",
            OperationsWarehouseRoles.Receiving | OperationsWarehouseRoles.Storage, false, null, null, "America/New_York", ct);
        Guid lim = await WarehouseAsync(lima, "LIM", "San Isidro office", "Av. Arequipa 3500", "San Isidro", "Lima", null, "PE",
            OperationsWarehouseRoles.PickupPoint, false, null, null, "America/Lima", ct);
        await ServicesAsync(new[] { "air_standard" }, OperationsWeightUnit.Kilogram, OperationsDimensionUnit.Centimetre, ct);
        await PriceAsync("air_standard", mia, null, null, Bands((0, OperationsBandKind.PerUnit, 9.50m)), 20m, ct);

        Guid rosa = await CustomerAsync("Rosa", "Quispe", null, "rosa", LanguageCode.Es, ct);
        string session = await SessionAsync(rosa, ct);
        DateOnly today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        Guid camera = await PreAlertAsync(session, mia, "Amazon", "TBA401122334455", "Amazon", "Cámara instantánea", 79.00m, today.AddDays(-1), ct);
        await ShipmentAsync(session, mia, Pickup("Rosa Quispe", "+51955443322", lim, "PE", "air_standard"), new[] { camera }, ct);
        (Guid delivery, Dictionary<string, Guid> parcels) = await DeliveryAsync(mia, "Amazon", new[] { "TBA401122334455" }, ct);
        await CheckAsync(parcels["TBA401122334455"], "intact", 0.9m, 20m, 15m, 12m, ct);
        await Use<CloseOperationsDeliveryUseCase>(async useCase => Ok(await useCase.ExecuteAsync(_staff, new CloseOperationsDeliveryRequest { DeliveryId = delivery }, ct), "close delivery"));
    }

    // =====================================================
    // SETUP STEPS
    // =====================================================
    private Task SettingsAsync(string currency, OperationsWeightUnit weight, OperationsDimensionUnit length, string timeZone, CancellationToken ct) =>
        Use<UpdateOperationsTenantSettingsUseCase>(async useCase => Ok(await useCase.ExecuteAsync(_staff, new UpdateOperationsTenantSettingsRequest
        {
            CurrencyCode = currency,
            WeightUnit = weight,
            DimensionUnit = length,
            DeliveryModes = OperationsDeliveryModes.Pickup | OperationsDeliveryModes.HomeDelivery,
            ShipmentPrefix = "SH",
            ReceivingPrefix = "WR",
            InvoicePrefix = "INV",
            PaymentPrefix = "PAY",
            TimeZone = timeZone
        }, ct), "settings"));

    private Task<Guid> BranchAsync(string code, string name, string line1, string city, string? region, string? postalCode, string country, OperationsBranchSide side,
        CancellationToken ct) =>
        Use<SaveOperationsBranchUseCase, Guid>(async useCase =>
        {
            SaveOperationsBranchResponse response = await useCase.ExecuteAsync(_staff, new SaveOperationsBranchRequest
            {
                Code = code, Name = name, Line1 = line1, City = city, Region = region, PostalCode = postalCode, CountryCode = country, Side = side,
                Status = OperationsLocationStatus.Active, ShowOnSite = true, Phone = "+13055550100"
            }, ct);
            Ok(response.Succeeded, response.Error, "branch " + code);
            return response.BranchId;
        });

    private Task<Guid> WarehouseAsync(Guid branch, string code, string name, string line1, string city, string? region, string? postalCode, string country,
        OperationsWarehouseRoles roles, bool home, IReadOnlyList<string>? area, Guid? hub, string timeZone, CancellationToken ct, string? partner = null) =>
        Use<SaveOperationsWarehouseUseCase, Guid>(async useCase =>
        {
            SaveOperationsWarehouseResponse response = await useCase.ExecuteAsync(_staff, new SaveOperationsWarehouseRequest
            {
                BranchId = branch, Code = code, Name = name, Line1 = line1, City = city, Region = region, PostalCode = postalCode, CountryCode = country,
                Status = OperationsLocationStatus.Active, Roles = roles, AcceptingDeliveries = roles.HasFlag(OperationsWarehouseRoles.Receiving), TimeZone = timeZone,
                DeliversHome = home, HomeArea = area, HubWarehouseId = hub,
                OperatedBy = partner is null ? OperationsDestinationOperator.Company : OperationsDestinationOperator.Partner,
                PartnerName = partner, PartnerPhone = partner is null ? null : "+5114445566"
            }, ct);
            Ok(response.Succeeded, response.Error, "warehouse " + code);
            return response.WarehouseId;
        });

    private Task<Guid> ShelfAsync(Guid warehouse, string code, OperationsStorageZone zone, CancellationToken ct) =>
        Use<SaveOperationsStorageLocationUseCase, Guid>(async useCase =>
        {
            SaveOperationsStorageLocationResponse response = await useCase.ExecuteAsync(_staff,
                new SaveOperationsStorageLocationRequest { WarehouseId = warehouse, Code = code, Zone = zone }, ct);
            Ok(response.Succeeded, response.Error, "shelf " + code);
            return response.LocationId;
        });

    private Task ServicesAsync(IReadOnlyCollection<string> offered, OperationsWeightUnit weight, OperationsDimensionUnit length, CancellationToken ct) =>
        Use<SaveOperationsShippingServicesUseCase>(async useCase => Ok(await useCase.ExecuteAsync(_staff, new SaveOperationsShippingServicesRequest
        {
            Services = Vixly.Contracts.Platform.Catalog.PlatformShippingServices.All.Select(service => new OperationsShippingServiceRow(service.Code, service.Mode,
                offered.Contains(service.Code), null, service.DefaultMinDays, service.DefaultMaxDays,
                service.DefaultVolumetricDivisor is { } divisor ? OperationsUnits.ToShownDivisor(divisor, length, weight) : null, service.DefaultVolumetricDivisor is not null)).ToList()
        }, ct), "services"));

    private Task PriceAsync(string service, Guid origin, Guid? destination, string? customerCode, IReadOnlyList<OperationsRateBand> bands, decimal minimum, CancellationToken ct) =>
        Use<SaveOperationsPriceListUseCase>(async useCase => Ok(await useCase.ExecuteAsync(_staff, new SaveOperationsPriceListRequest
        {
            ServiceCode = service, OriginWarehouseId = origin, DestinationWarehouseId = destination, CustomerCode = customerCode, Bands = bands, MinimumCharge = minimum,
            Customs = OperationsCustomsPolicy.TransportOnly, Active = true
        }, ct), "price list " + service));

    private Task ExtraAsync(string name, decimal amount, string words, string country, CancellationToken ct) =>
        Use<SaveOperationsChargeExtraUseCase>(async useCase => Ok(await useCase.ExecuteAsync(_staff, new SaveOperationsChargeExtraRequest
        {
            Name = name, IsGovernmentFee = true, Amount = amount, Calculation = OperationsExtraCalculation.Fixed, Per = OperationsExtraPer.Item,
            Trigger = OperationsExtraTrigger.Declared, Words = words, CountryCode = country, Active = true
        }, ct), "extra"));

    // Run again, the same product is changed rather than added a second time.
    private async Task InsuranceAsync(OperationsTransportMode mode, string name, int position, decimal percent, decimal minimum, decimal maximum, string? pitch, CancellationToken ct)
    {
        Guid? existing = await WithAsync(async services => (await services.GetRequiredService<IOperationsChargeRepository>().ListInsurancePlansAsync(ct))
            .FirstOrDefault(plan => plan.Mode == mode && (plan.Name == name || plan.Name == mode + " insurance"))?.Id);
        await InsuranceSaveAsync(mode, existing, name, position, percent, minimum, maximum, pitch, ct);
    }

    private Task InsuranceSaveAsync(OperationsTransportMode mode, Guid? existing, string name, int position, decimal percent, decimal minimum, decimal maximum, string? pitch,
        CancellationToken ct) =>
        Use<SaveOperationsInsurancePlanUseCase>(async useCase => Ok(await useCase.ExecuteAsync(_staff, new SaveOperationsInsurancePlanRequest
        {
            Mode = mode, PlanId = existing, Name = name, Position = position, PitchEn = pitch, Method = OperationsInsuranceMethod.Percent, Percent = percent, MinimumPremium = minimum, MaxValue = maximum,
            Active = true
        }, ct), "insurance"));

    // Slice 5E: shipping supplies sold at the counter, one with a supplier barcode and a lower price from 10. Also added to a demo company seeded before 5E (TopUpAsync).
    private async Task SuppliesAsync(CancellationToken ct)
    {
        await SupplyAsync("BOX-S", "Small box 30 x 20 x 15 cm", "Petite boîte 30 x 20 x 15 cm", "Caja pequeña 30 x 20 x 15 cm", null, OperationsSupplyCategory.Boxes, 1.75m, null, null, ct);
        await SupplyAsync("BOX-M", "Medium box 40 x 30 x 30 cm", "Boîte moyenne 40 x 30 x 30 cm", "Caja mediana 40 x 30 x 30 cm", null, OperationsSupplyCategory.Boxes, 2.50m, "012345678905", (10, 2.00m), ct);
        await SupplyAsync("BOX-L", "Large box 60 x 40 x 40 cm", "Grande boîte 60 x 40 x 40 cm", "Caja grande 60 x 40 x 40 cm", null, OperationsSupplyCategory.Boxes, 3.75m, null, null, ct);
        await SupplyAsync("TAPE-48", "Packing tape 48 mm x 50 m", "Ruban d’emballage 48 mm x 50 m", "Cinta de embalaje 48 mm x 50 m", "roll", OperationsSupplyCategory.Tape, 4.50m, null, null, ct);
        await SupplyAsync("WRAP-12", "Bubble wrap 30 cm x 3 m", "Papier bulle 30 cm x 3 m", "Plástico de burbujas 30 cm x 3 m", "roll", OperationsSupplyCategory.Padding, 6.00m, null, null, ct);
        await SupplyAsync("BAG-POLY", "Poly mailer 35 x 45 cm", "Enveloppe plastique 35 x 45 cm", "Sobre plástico 35 x 45 cm", null, OperationsSupplyCategory.Bags, 1.25m, null, (10, 1.00m), ct);
    }

    private Task SupplyAsync(string code, string en, string fr, string es, string? unit, OperationsSupplyCategory category, decimal price, string? barcode,
        (int From, decimal Price)? step, CancellationToken ct) =>
        Use<SaveOperationsSupplyUseCase>(async useCase => Ok(await useCase.ExecuteAsync(_staff, new SaveOperationsSupplyRequest
        {
            Code = code, NameEn = en, NameFr = fr, NameEs = es, Unit = unit, Category = category, Taxed = true, Active = true,
            Barcodes = barcode is null ? Array.Empty<string>() : new[] { barcode },
            Price = price,
            Breaks = step is { } quantity ? new[] { new OperationsSupplyBreak(quantity.From, quantity.Price) } : Array.Empty<OperationsSupplyBreak>()
        }, ct), "supply " + code));

    private Task<Guid> PolicyAsync(OperationsPolicyTerms terms, CancellationToken ct) =>
        Use<SaveOperationsInsurancePolicyUseCase, Guid>(async useCase =>
        {
            SaveOperationsInsurancePolicyResponse response = await useCase.ExecuteAsync(_staff, new SaveOperationsInsurancePolicyRequest { Terms = terms }, ct);
            Ok(response.Succeeded, response.Error, "policy " + terms.PolicyNumber);
            return response.PolicyId;
        });

    private static IReadOnlyList<OperationsRateBand> Bands(params (decimal From, OperationsBandKind Kind, decimal Amount)[] bands) =>
        bands.Select(band => new OperationsRateBand(band.From, band.Kind, band.Amount)).ToList();

    // =====================================================
    // CUSTOMERS
    // =====================================================
    private Task<Guid> CustomerAsync(string first, string last, string? company, string mailbox, LanguageCode language, CancellationToken ct) =>
        WithAsync(async services =>
        {
            EmailAddress email = EmailAddress.Create(mailbox + "@" + _slug + ".example");
            IOperationsCustomerRepository customers = services.GetRequiredService<IOperationsCustomerRepository>();

            if (await customers.FindByEmailAsync(email, ct) is { } existing)
            {
                return existing.Id;
            }

            DateTimeOffset utcNow = _timeProvider.GetUtcNow();
            OperationsCustomer customer = OperationsCustomer.SignUp(_tenant, first, last, company, email, "+15555550123", language,
                services.GetRequiredService<IOperationsPasswordHasher>().Hash(_options.Password), utcNow);
            customer.ConfirmEmail(await services.GetRequiredService<OperationsNumbering>().NextCustomerCodeAsync(ct), utcNow);
            customers.Add(customer);
            Ok(await services.GetRequiredService<IOperationsUnitOfWork>().SaveChangesAsync(ct), null, "customer " + mailbox);
            return customer.Id;
        });

    private Task<string> SessionAsync(Guid customerId, CancellationToken ct) =>
        WithAsync(async services =>
        {
            OperationsCustomer customer = (await services.GetRequiredService<IOperationsCustomerRepository>().GetAsync(customerId, ct))!;
            string token = services.GetRequiredService<OperationsCustomerAccounts>().StartSession(customer, _timeProvider.GetUtcNow());
            Ok(await services.GetRequiredService<IOperationsUnitOfWork>().SaveChangesAsync(ct), null, "session");
            return token;
        });

    private Task<string> CodeAsync(Guid customerId, CancellationToken ct) =>
        WithAsync(async services => (await services.GetRequiredService<IOperationsCustomerRepository>().GetAsync(customerId, ct))!.Code!);

    private Task<Guid> PreAlertAsync(string session, Guid warehouse, string carrier, string tracking, string store, string contents, decimal value, DateOnly expected,
        CancellationToken ct) =>
        Use<CreateMyOperationsPreAlertUseCase, Guid>(async useCase =>
        {
            CreateMyOperationsPreAlertResponse response = await useCase.ExecuteAsync(OperationsAuthorizationContext.ForTenant(_tenant), new CreateMyOperationsPreAlertRequest
            {
                SessionToken = session, WarehouseId = warehouse, Carrier = carrier, TrackingNumber = tracking, Store = store, Contents = contents, DeclaredValue = value,
                ExpectedDate = expected
            }, ct);
            Ok(response.Succeeded, response.Error, "pre-alert " + tracking);
            return response.PreAlertId;
        });

    private Task<Guid> ShipmentAsync(string session, Guid origin, OperationsShipmentDraft instructions, IReadOnlyList<Guid> preAlerts, CancellationToken ct) =>
        Use<CreateMyOperationsShipmentUseCase, Guid>(async useCase =>
        {
            CreateMyOperationsShipmentResponse response = await useCase.ExecuteAsync(OperationsAuthorizationContext.ForTenant(_tenant), new CreateMyOperationsShipmentRequest
            {
                // Slice 5C: the customer declares as many parcels as the packages announced in it.
                SessionToken = session, OriginWarehouseId = origin, Instructions = instructions with { ParcelCount = instructions.ParcelCount ?? Math.Max(1, preAlerts.Count) },
                PreAlertIds = preAlerts, RequestKey = Key()
            }, ct);
            Ok(response.Succeeded, response.Error, "shipment for " + instructions.RecipientName);
            return response.ShipmentId;
        });

    private static OperationsShipmentDraft Home(string line1, string city, string country, string recipient, string phone, Guid destination, string service, bool insured) =>
        new("home", null, line1, null, city, null, null, country, recipient, phone, false, false, insured, destination, insured, service);

    private static OperationsShipmentDraft Pickup(string recipient, string phone, Guid destination, string country, string service) =>
        new("pickup", null, null, null, null, null, null, country, recipient, phone, false, false, false, destination, false, service);

    // =====================================================
    // THE DOCK AND THE INSPECTION STATION
    // =====================================================
    private async Task<(Guid Delivery, Dictionary<string, Guid> Parcels)> DeliveryAsync(Guid warehouse, string carrier, IReadOnlyList<string> barcodes, CancellationToken ct)
    {
        Guid delivery = await Use<OpenOperationsDeliveryUseCase, Guid>(async useCase =>
        {
            OpenOperationsDeliveryResponse response = await useCase.ExecuteAsync(_staff,
                new OpenOperationsDeliveryRequest { WarehouseId = warehouse, Carrier = carrier, CommandKey = Key() }, ct);
            Ok(response.Succeeded, response.Error, "open delivery " + carrier);
            return response.DeliveryId;
        });
        Dictionary<string, Guid> parcels = new(StringComparer.Ordinal);

        foreach (string barcode in barcodes)
        {
            parcels[barcode] = await Use<ScanOperationsParcelUseCase, Guid>(async useCase =>
            {
                ScanOperationsParcelResponse response = await useCase.ExecuteAsync(_staff,
                    new ScanOperationsParcelRequest { DeliveryId = delivery, Barcode = barcode, ScanKey = Key(), PortalSlug = _slug }, ct);
                Ok(response.Succeeded, response.Error, "scan " + barcode);
                return response.ParcelId;
            });
        }

        return (delivery, parcels);
    }

    private Task CheckAsync(Guid parcel, string condition, decimal weight, decimal length, decimal width, decimal height, CancellationToken ct, string? flag = null) =>
        Use<CheckOperationsParcelUseCase>(async useCase =>
        {
            List<OperationsParcelPhotoUpload> photos = new() { new OperationsParcelPhotoUpload("package", Png(0xB8, 0x8B, 0x5A)), new OperationsParcelPhotoUpload("label", Png(0xF2, 0xF2, 0xF2)) };

            if (condition != "intact")
            {
                photos.Add(new OperationsParcelPhotoUpload("damage", Png(0x8E, 0x3B, 0x2F)));
            }

            Ok(await useCase.ExecuteAsync(_staff, new CheckOperationsParcelRequest
            {
                ParcelId = parcel, Condition = condition, Weight = weight, Length = length, Width = width, Height = height, Photos = photos, CommandKey = Key(),
                Flag = flag is not null, FlagReason = flag
            }, ct), "check");
        });

    private Task ShelveAsync(Guid parcel, Guid shelf, CancellationToken ct) =>
        Use<PutAwayOperationsParcelUseCase>(async useCase => Ok(await useCase.ExecuteAsync(_staff,
            new PutAwayOperationsParcelRequest { ParcelId = parcel, StorageLocationId = shelf, CommandKey = Key() }, ct), "put away"));

    // =====================================================
    // PLUMBING
    // =====================================================
    private Task Use<TUseCase>(Func<TUseCase, Task> work)
        where TUseCase : notnull =>
        WithAsync<bool>(async services =>
        {
            await work(services.GetRequiredService<TUseCase>());
            return true;
        });

    private Task<T> Use<TUseCase, T>(Func<TUseCase, Task<T>> work)
        where TUseCase : notnull =>
        WithAsync(services => work(services.GetRequiredService<TUseCase>()));

    private Task<T> QueryAsync<T>(Func<OperationsDbContext, Task<T>> work) =>
        WithAsync(services => work(services.GetRequiredService<OperationsDbContext>()));

    // A fresh scope for each step, in the company, as each request of a real day would have.
    private async Task<T> WithAsync<T>(Func<IServiceProvider, Task<T>> work)
    {
        await using AsyncServiceScope scope = _scopes.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(_tenant);
        return await work(scope.ServiceProvider);
    }

    private static void Ok(dynamic response, string step) => Ok((bool)response.Succeeded, (Error?)response.Error, step);

    private static void Ok(bool succeeded, Error? error, string step)
    {
        if (!succeeded)
        {
            throw new InvalidOperationException("Demo data stopped at \"" + step + "\": " + (error?.Code ?? "not saved") + ".");
        }
    }

    private static string Key() => Guid.NewGuid().ToString("N");

    // A small real PNG (a coloured card with a darker band, like a box with its label), so pages show pictures, not broken images.
    internal static byte[] Png(byte red, byte green, byte blue)
    {
        const int width = 160;
        const int height = 120;
        byte[] raw = new byte[height * ((width * 3) + 1)];

        for (int y = 0; y < height; y++)
        {
            int row = y * ((width * 3) + 1);

            for (int x = 0; x < width; x++)
            {
                bool band = y is > 40 and < 80 && x is > 30 and < 130;
                int at = row + 1 + (x * 3);
                (raw[at], raw[at + 1], raw[at + 2]) = band ? ((byte)(red / 2), (byte)(green / 2), (byte)(blue / 2)) : (red, green, blue);
            }
        }

        using MemoryStream compressed = new();

        using (ZLibStream zlib = new(compressed, CompressionLevel.Optimal, leaveOpen: true))
        {
            zlib.Write(raw);
        }

        using MemoryStream png = new();
        png.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        byte[] header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
        (header[8], header[9], header[10], header[11], header[12]) = (8, 2, 0, 0, 0);
        Chunk(png, "IHDR", header);
        Chunk(png, "IDAT", compressed.ToArray());
        Chunk(png, "IEND", Array.Empty<byte>());
        return png.ToArray();
    }

    private static void Chunk(Stream png, string type, byte[] data)
    {
        byte[] length = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
        png.Write(length);
        byte[] name = System.Text.Encoding.ASCII.GetBytes(type);
        png.Write(name);
        png.Write(data);
        byte[] crc = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crc, Crc(name, data));
        png.Write(crc);
    }

    private static uint Crc(byte[] name, byte[] data)
    {
        uint crc = 0xFFFFFFFF;

        foreach (byte value in name.Concat(data))
        {
            crc ^= value;

            for (int bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) != 0 ? 0xEDB88320 ^ (crc >> 1) : crc >> 1;
            }
        }

        return ~crc;
    }
}
