using Vixly.Operations.Application.Charges;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Application.Tests;

// =========================================================
// FILE: OperationsRatingEngineTests.cs
//
// PURPOSE:
// Proves the charges engine against the owner's answers (Slice 4), the bands and versions of Slice 4B and the routes, modes and insurance terms of Slice 4C.
//
// RESPONSIBILITIES:
// - Bands set by the company: flat, per unit, base plus per unit; rounding up; volumetric weight; the minimum
// - Ocean by size; item prices from the company's item types; consolidation
// - Extras: per item from the declaration, a percentage, staff-added, a government fee that cannot be removed, limited to a mode, a destination and a delivery mode
// - Insurance by percent and bands, with its terms; the price list picked by route and customer; versions with effective dates; destinations; ground flat prices
//
// IMPORTANT NOTES:
// - No database; a pound and inch company unless said
// =========================================================
public sealed class OperationsRatingEngineTests
{
    private const decimal Kg = 0.45359237m;

    private static readonly DateTimeOffset Now = new(2026, 10, 19, 15, 0, 0, TimeSpan.Zero);

    private static readonly Guid Tenant = Guid.NewGuid();

    private static readonly Guid Cayes = Guid.NewGuid();

    private static readonly Guid Jeremie = Guid.NewGuid();

    private static readonly Guid Miami = Guid.NewGuid();

    [Fact]
    public void CompanyBands_FlatThenPerUnit_AsInV3_WithRoundingUpAndNoGaps()
    {
        // The V3 air tiers of the owner's screen: 0-10 flat 50, 11-20 flat 65, 21-30 flat 75, then 4.75 a pound.
        OperationsRateCard card = Card(Flat(0, 50), Flat(11, 65), Flat(21, 75), PerUnit(31, 4.75m));

        OperationsRating rating = Rate(card, Piece(10.4m), Piece(10m), Piece(20m), Piece(30m), Piece(35m), Piece(1m, 20, 16, 12));

        // 10.4 lb pays for 11 (no gap between bands); 10 lb typed and stored in kg stays 10; a light big box pays its volumetric weight (24, flat 75).
        Assert.Equal(new[] { 11m, 10m, 20m, 30m, 35m, 24m }, rating.Lines.Select(line => line.Basis!.Value).ToArray());
        Assert.Equal(new[] { 65m, 50m, 65m, 75m, 166.25m, 75m }, rating.Lines.Select(line => line.Amount).ToArray());
    }

    [Fact]
    public void BasePlusPerUnit_ChargesAboveTheBandStart_AndAnyNumberOfBands()
    {
        OperationsRateCard card = Card(Flat(0, 50), new OperationsRateBand(30, OperationsBandKind.BasePlusPerUnit, 4.75m, 75m));

        Assert.Equal(new[] { 75m, 98.75m }, Rate(card, Piece(30m), Piece(35m)).Lines.Select(line => line.Amount).ToArray());
        Assert.Equal(new[] { 30m }, Rate(Card(PerUnit(0, 2.5m)), Piece(12m)).Lines.Select(line => line.Amount).ToArray());
        OperationsRateBand[] many = Enumerable.Range(0, 40).Select(index => Flat(index * 5, 10 + index)).ToArray();
        Assert.Equal(17m, Rate(Card(many), Piece(37m)).Total);
    }

    [Fact]
    public void Bands_StartAtZero_OnceEach_AndTheBaseGoesWithItsKind()
    {
        Assert.Equal(OperationsChargeErrors.BandsInvalid, Assert.Throws<DomainException>(() => Card(Flat(1, 50))).Error);
        Assert.Equal(OperationsChargeErrors.BandsInvalid, Assert.Throws<DomainException>(() => Card(Flat(0, 50), Flat(0, 60))).Error);
        Assert.Equal(OperationsChargeErrors.BandsInvalid, Assert.Throws<DomainException>(() => Card(new OperationsRateBand(0, OperationsBandKind.BasePlusPerUnit, 1m))).Error);
        Assert.Equal(OperationsChargeErrors.BandsInvalid, Assert.Throws<DomainException>(() => Card(new OperationsRateBand(0, OperationsBandKind.Flat, 1m, 5m))).Error);
        Assert.Equal(OperationsChargeErrors.BandsInvalid, Assert.Throws<DomainException>(() => Card()).Error);
    }

    [Fact]
    public void Minimum_ServiceFee_Consolidation_AndOceanBySizeWithItemTypes()
    {
        OperationsRateCard air = Card(new[] { Flat(0, 15), PerUnit(11, 2.5m) }, minimum: 40m, serviceFee: 3m);
        Assert.Equal(new[] { 15m, 25m, 3m }, Rate(air, Piece(3m)).Lines.Select(line => line.Amount).ToArray());

        OperationsItemType barrel = OperationsItemType.Create(Tenant, "Barrel", "barrel, drum", Now);
        OperationsRateCard ocean = Card(new[] { PerUnit(0, 12m) }, service: "ocean_consolidated", items: new[] { new OperationsRateItem(barrel.Id, 85m) },
            consolidation: 5m);
        OperationsRating rating = RateWith(ocean, new[] { Piece(40m, 30, 30, 30), Piece(200m, 22, 22, 36, item: barrel.Id) }, consolidated: true, types: new[] { barrel });

        // 30 x 30 x 30 in = 15.625 ft³, rounded up to 15.63; the barrel pays its price whatever it weighs.
        Assert.Equal(new[] { OperationsChargeLineKind.Freight, OperationsChargeLineKind.Item, OperationsChargeLineKind.Consolidation }, rating.Lines.Select(line => line.Kind).ToArray());
        Assert.Equal(new[] { 187.56m, 85m, 5m }, rating.Lines.Select(line => line.Amount).ToArray());
        Assert.Equal("Barrel", rating.Lines[1].Label);

        // Air goes by weight only: an air list takes no item price, and a barrel flown is priced by its weight.
        Assert.Equal(OperationsChargeErrors.ItemsInvalid, Assert.Throws<DomainException>(() => Card(new[] { PerUnit(0, 2.5m) }, items: new[] { new OperationsRateItem(barrel.Id, 85m) })).Error);
        Assert.Equal(OperationsRateBasis.Weight, Card(PerUnit(0, 2.5m)).Basis);
        Assert.Equal(new[] { OperationsChargeLineKind.Freight }, RateWith(Card(PerUnit(0, 2.5m)), new[] { Piece(10m, item: barrel.Id) }, types: new[] { barrel }).Lines
            .Select(line => line.Kind).ToArray());
    }

    [Fact]
    public void Extras_ByDeclaration_Percentage_StaffAdded_GovernmentFee_ModeDestinationAndDelivery()
    {
        OperationsRateCard card = Card(PerUnit(0, 2.5m));
        OperationsChargeExtra conatel = Extra("CONATEL fee", 10m, OperationsExtraCalculation.Fixed, OperationsExtraPer.Item, OperationsExtraTrigger.Declared, words: "iphone", country: "HT", government: true);
        OperationsChargeExtra fuel = Extra("Fuel", 3m, OperationsExtraCalculation.PercentOfFreight, OperationsExtraPer.Shipment, OperationsExtraTrigger.Always);
        OperationsChargeExtra handling = Extra("Special handling", 7m, OperationsExtraCalculation.Fixed, OperationsExtraPer.Shipment, OperationsExtraTrigger.Manual);
        OperationsChargeExtra jeremieDelivery = Extra("Jérémie delivery", 12m, OperationsExtraCalculation.Fixed, OperationsExtraPer.Shipment, OperationsExtraTrigger.Always, to: Jeremie,
            delivery: OperationsDeliveryModes.HomeDelivery);
        OperationsChargeExtra barrelFee = Extra("Port fee", 4m, OperationsExtraCalculation.Fixed, OperationsExtraPer.Shipment, OperationsExtraTrigger.Always, mode: OperationsTransportMode.Ocean);
        OperationsChargeExtra[] extras = { conatel, fuel, handling, jeremieDelivery, barrelFee };
        OperationsRatingPiece phones = Piece(4m, contents: "Two iPhones and a case");

        OperationsRating toCayes = RateWith(card, new[] { phones, Piece(6m, contents: "Shoes") }, extras: extras);
        Assert.Equal(new[] { "CONATEL fee", "Fuel" }, toCayes.Lines.Where(line => line.Kind == OperationsChargeLineKind.Extra).Select(line => line.Label).ToArray());
        Assert.Equal(0.75m, toCayes.Lines.Single(line => line.Label == "Fuel").Amount);

        // Staff give two phones and add the handling; a government fee is never removed.
        OperationsRating quoted = RateWith(card, new[] { phones }, extras: extras, quantities: new Dictionary<Guid, int> { [conatel.Id] = 2, [handling.Id] = 1, [fuel.Id] = 0 });
        Assert.Equal(new[] { 20m, 0.30m, 7m }, quoted.Lines.Where(line => line.Kind == OperationsChargeLineKind.Extra).Select(line => line.Amount).ToArray());
        Assert.Contains(RateWith(card, new[] { phones }, extras: extras, quantities: new Dictionary<Guid, int> { [conatel.Id] = 0 }).Lines, line => line.Label == "CONATEL fee");

        // A home delivery fee in Jérémie only.
        Assert.Contains(RateWith(card, new[] { phones }, extras: extras, to: Jeremie, delivery: OperationsDeliveryModes.HomeDelivery).Lines, line => line.Label == "Jérémie delivery");
        Assert.DoesNotContain(RateWith(card, new[] { phones }, extras: extras, to: Jeremie, delivery: OperationsDeliveryModes.Pickup).Lines, line => line.Label == "Jérémie delivery");
        Assert.DoesNotContain(RateWith(card, new[] { phones }, extras: extras, to: Cayes, delivery: OperationsDeliveryModes.HomeDelivery).Lines, line => line.Label == "Jérémie delivery");

        // An ocean fee never shows on air; it does on an ocean list.
        Assert.DoesNotContain(toCayes.Lines, line => line.Label == "Port fee");
        Assert.Contains(RateWith(Card(new[] { PerUnit(0, 12m) }, service: "ocean_consolidated"), new[] { Piece(1m, 10, 10, 10) }, extras: extras).Lines, line => line.Label == "Port fee");
    }

    [Fact]
    public void Insurance_ByPercent_ByBands_AndScheduledFromADay()
    {
        OperationsRateCard card = Card(PerUnit(0, 1m));
        OperationsInsurancePlan plan = OperationsInsurancePlan.Create(Tenant, OperationsTransportMode.Air, new OperationsInsuranceTerms(OperationsInsuranceMethod.Percent, 3m, 5m, Array.Empty<OperationsInsuranceBand>(), null), null, Now);
        plan.Schedule(new OperationsInsuranceTerms(OperationsInsuranceMethod.Bands, null, null, new[] { new OperationsInsuranceBand(100, 3m), new OperationsInsuranceBand(500, 10m) }, null), Now.AddDays(10), Now);

        Assert.Equal(30m, Premium(RateWith(card, new[] { Piece(1m, value: 400m), Piece(1m, value: 600m) }, insurance: plan, insured: true)));
        Assert.Equal(10m, Premium(RateWith(card, new[] { Piece(1m, value: 250m) }, insurance: plan, insured: true, at: Now.AddDays(11))));
        Assert.True(RateWith(card, new[] { Piece(1m, value: 600m) }, insurance: plan, insured: true, at: Now.AddDays(11)).InsuranceRefused);
        Assert.True(RateWith(card, new[] { Piece(1m, value: 100m) }, insurance: null, insured: true).InsuranceRefused);
    }

    [Fact]
    public void PriceLists_ArePickedByRouteAndCustomer_TheMostSpecificFirst()
    {
        Guid valued = Guid.NewGuid();
        Guid other = Guid.NewGuid();
        OperationsRateCard everywhere = Card(PerUnit(0, 2.5m));
        OperationsRateCard jeremie = Card(new[] { PerUnit(0, 3m) }, to: Jeremie);
        OperationsRateCard miamiJeremie = Card(new[] { PerUnit(0, 3.5m) }, from: Miami, to: Jeremie);
        OperationsRateCard fromMiami = Card(new[] { PerUnit(0, 2.75m) }, from: Miami);
        OperationsRateCard valuedEverywhere = Card(new[] { PerUnit(0, 2m) }, customer: valued);
        OperationsRateCard otherJeremie = Card(new[] { PerUnit(0, 1m) }, customer: other, to: Jeremie);
        OperationsRateCard[] cards = { everywhere, jeremie, miamiJeremie, fromMiami, valuedEverywhere, otherJeremie };

        Assert.Same(miamiJeremie, OperationsRateCardChoice.Pick(cards, "air_standard", Miami, Jeremie, null, Now));
        Assert.Same(jeremie, OperationsRateCardChoice.Pick(cards, "air_standard", Cayes, Jeremie, null, Now));
        Assert.Same(jeremie, OperationsRateCardChoice.Pick(cards, "air_standard", null, Jeremie, null, Now));
        Assert.Same(fromMiami, OperationsRateCardChoice.Pick(cards, "air_standard", Miami, Cayes, null, Now));
        Assert.Same(everywhere, OperationsRateCardChoice.Pick(cards, "air_standard", null, Cayes, null, Now));
        Assert.Same(valuedEverywhere, OperationsRateCardChoice.Pick(cards, "air_standard", Miami, Jeremie, valued, Now));
        Assert.Same(otherJeremie, OperationsRateCardChoice.Pick(cards, "air_standard", Miami, Jeremie, other, Now));
        Assert.Null(OperationsRateCardChoice.Pick(cards, "air_express", Miami, Jeremie, valued, Now));
        miamiJeremie.ChangeStatus(false, Now);
        Assert.Same(jeremie, OperationsRateCardChoice.Pick(cards, "air_standard", Miami, Jeremie, null, Now));

        // A list that starts next month never hides the one in force today.
        OperationsRateCard cayesLater = OperationsRateCard.Create(Tenant, "air_standard", null, Cayes, null, OperationsCustomsPolicy.DutiesIncluded, Prices(PerUnit(0, 4m)), Now.AddDays(30), Now);
        OperationsRateCard[] withLater = { everywhere, cayesLater };
        Assert.Same(everywhere, OperationsRateCardChoice.Pick(withLater, "air_standard", null, Cayes, null, Now));
        Assert.Same(cayesLater, OperationsRateCardChoice.Pick(withLater, "air_standard", null, Cayes, null, Now.AddDays(31)));
        Assert.Equal((OperationsCustomsPolicy.DutiesIncluded, OperationsCustomsPolicy.TransportOnly), (cayesLater.Customs, everywhere.Customs));
    }

    [Fact]
    public void Ground_ByWeight_AFlatPricePerPackageOrShipment_OrBoth()
    {
        OperationsRatePrices flats = new(Array.Empty<OperationsRateBand>(), Array.Empty<OperationsRateItem>(), null, null, null, 15m, 5m);
        OperationsRateCard flat = OperationsRateCard.Create(Tenant, "ground", null, null, null, OperationsCustomsPolicy.TransportOnly, flats, null, Now);
        OperationsRating two = Rate(flat, Piece(3m), Piece(40m));

        Assert.Equal(new[] { 15m, 15m, 5m }, two.Lines.Select(line => line.Amount).ToArray());
        Assert.Equal(OperationsChargeLineKind.ShipmentFreight, two.Lines[^1].Kind);

        OperationsRateCard both = OperationsRateCard.Create(Tenant, "ground", null, null, null, OperationsCustomsPolicy.TransportOnly,
            new OperationsRatePrices(new[] { PerUnit(0, 1m) }, Array.Empty<OperationsRateItem>(), null, null, null, 10m), null, Now);
        Assert.Equal(22m, Rate(both, Piece(12m)).Total);

        // Only a price per shipment: one line, never a 0.00 line per package; a flat price of zero never stands in for the bands.
        OperationsRateCard perShipment = OperationsRateCard.Create(Tenant, "ground", null, null, null, OperationsCustomsPolicy.TransportOnly,
            new OperationsRatePrices(Array.Empty<OperationsRateBand>(), Array.Empty<OperationsRateItem>(), null, null, null, null, 25m), null, Now);
        Assert.Equal(new[] { OperationsChargeLineKind.ShipmentFreight }, Rate(perShipment, Piece(3m), Piece(9m)).Lines.Select(line => line.Kind).ToArray());
        Assert.Equal(OperationsChargeErrors.BandsInvalid, Assert.Throws<DomainException>(() => OperationsRateCard.Create(Tenant, "ground", null, null, null,
            OperationsCustomsPolicy.TransportOnly, new OperationsRatePrices(Array.Empty<OperationsRateBand>(), Array.Empty<OperationsRateItem>(), null, null, null, 0m, 0m), null, Now)).Error);

        // Flat prices are a ground choice; air bands cannot be left out.
        Assert.Equal(OperationsChargeErrors.BandsInvalid, Assert.Throws<DomainException>(() =>
            OperationsRateCard.Create(Tenant, "air_standard", null, null, null, OperationsCustomsPolicy.TransportOnly, flats, null, Now)).Error);
        Assert.Equal(OperationsChargeErrors.BandsInvalid, Assert.Throws<DomainException>(() =>
            OperationsRateCard.Create(Tenant, "ground", null, null, null, OperationsCustomsPolicy.TransportOnly, Prices(), null, Now)).Error);
    }

    [Fact]
    public void Versions_StartOnTheirDay_StartedOnesNeverChange_ScheduledOnesCanBeRemoved()
    {
        OperationsRateCard card = Card(PerUnit(0, 2.5m));
        OperationsPriceVersion november = card.Schedule(Prices(PerUnit(0, 3m)), Now.AddDays(13), Now);

        Assert.Equal(25m, Rate(card, Piece(10m)).Total);
        Assert.Equal(30m, RateWith(card, new[] { Piece(10m) }, at: Now.AddDays(14)).Total);

        // Scheduling again for the same moment replaces the scheduled version; the started one stays.
        card.Schedule(Prices(PerUnit(0, 3.5m)), Now.AddDays(13), Now);
        Assert.Equal(2, card.Versions.Count);
        Assert.Equal(35m, RateWith(card, new[] { Piece(10m) }, at: Now.AddDays(14)).Total);

        Assert.Equal(OperationsChargeErrors.VersionStarted, Assert.Throws<DomainException>(() => card.RemoveScheduled(card.Versions[0].Id, Now)).Error);
        Assert.Equal(OperationsChargeErrors.VersionNotFound, Assert.Throws<DomainException>(() => card.RemoveScheduled(november.Id, Now)).Error);
        card.RemoveScheduled(card.Versions[1].Id, Now);
        Assert.Equal(25m, RateWith(card, new[] { Piece(10m) }, at: Now.AddDays(14)).Total);
        Assert.Equal(OperationsChargeErrors.EffectiveInvalid, Assert.Throws<DomainException>(() => card.Schedule(Prices(PerUnit(0, 1m)), Now.AddDays(-3), Now)).Error);

        // Before its first version a list has no price.
        OperationsRateCard later = OperationsRateCard.Create(Tenant, "air_standard", null, null, null, OperationsCustomsPolicy.TransportOnly, Prices(PerUnit(0, 1m)), Now.AddDays(5), Now);
        Assert.Null(OperationsRatingEngine.Rate(Input(later, new[] { Piece(1m) }, at: Now)));

        // Its only version cannot be removed, and the list is left as it was.
        Assert.Equal(OperationsChargeErrors.VersionStarted, Assert.Throws<DomainException>(() => later.RemoveScheduled(later.Versions[0].Id, Now)).Error);
        Assert.Single(later.Versions);
    }

    [Fact]
    public void EffectiveDays_StartAtMidnightInTheCompanysTimeZone()
    {
        OperationsTenantSettings settings = OperationsTenantSettings.CreateDefault(Tenant, Now);
        settings.ChangeTimeZone("America/Port-au-Prince", Now);

        Assert.Null(OperationsCharging.Start(new DateOnly(2026, 10, 19), settings, Now));
        Assert.Equal(new DateTimeOffset(2026, 11, 2, 0, 0, 0, TimeSpan.FromHours(-5)), OperationsCharging.Start(new DateOnly(2026, 11, 2), settings, Now));
        Assert.Equal(OperationsChargeErrors.EffectiveInvalid, Assert.Throws<DomainException>(() => OperationsCharging.Start(new DateOnly(2026, 10, 17), settings, Now)).Error);
        Assert.Equal(OperationsDestinationErrors.TimeZoneInvalid, Assert.Throws<DomainException>(() => settings.ChangeTimeZone("Mars/Olympus", Now)).Error);
    }

    [Fact]
    public void Destinations_OfferPickupOrHomeDeliveryInTheirArea_AndShipmentsGoThere()
    {
        OperationsWarehouse jeremie = OperationsWarehouse.Create(Tenant, Guid.NewGuid(), new OperationsLocationDetails("JER", "Jérémie", OperationsPostalAddress.Create("1 Rue", null, "Jérémie",
            null, null, "HT"), null, null, null, false), OperationsWarehouseRoles.PickupPoint, false, Now);
        jeremie.ChangeDestination(new OperationsDestinationTerms(true, null, OperationsDestinationOperator.Partner, "Grand'Anse Express", null, null, new[] { "Jérémie", "Dame-Marie" }), Now);
        OperationsWarehouse doral = OperationsWarehouse.Create(Tenant, Guid.NewGuid(), new OperationsLocationDetails("MIA", "Doral", OperationsPostalAddress.Create("1 Main", null, "Doral",
            "FL", "33122", "US"), null, null, null, false), OperationsWarehouseRoles.Receiving, true, Now);

        Assert.True(jeremie.IsDestination && jeremie.Covers("JEREMIE") && jeremie.Covers("dame marie") && !jeremie.Covers("Les Cayes"));
        Assert.True(doral.IsOrigin && !doral.IsDestination);
        Assert.Equal(jeremie.Id, OperationsShipmentInstructions.Pickup(jeremie, "Ana Pierre", "+50937123456", OperationsShipmentOptions.None).WarehouseId);
        Assert.Equal(jeremie.Id, OperationsShipmentInstructions.HomeDelivery(jeremie, OperationsPostalAddress.Create("1 Rue", null, "Dame-Marie", null, null, "HT"), "Ana Pierre",
            "+50937123456", OperationsShipmentOptions.None).WarehouseId);
        Assert.Throws<DomainException>(() => OperationsShipmentInstructions.HomeDelivery(jeremie, OperationsPostalAddress.Create("1 Rue", null, "Les Cayes", null, null, "HT"), "Ana Pierre",
            "+50937123456", OperationsShipmentOptions.None));
        Assert.Throws<DomainException>(() => OperationsShipmentInstructions.Pickup(doral, "Ana Pierre", "+50937123456", OperationsShipmentOptions.None));

        // The insurance acceptance: its time is renewed with each acceptance, kept by a change that gives none, gone when no longer insured.
        OperationsShipment shipment = OperationsShipment.Request(Tenant, Guid.NewGuid(), doral.Id,
            OperationsShipmentInstructions.Pickup(jeremie, "Ana Pierre", "+50937123456", OperationsShipmentOptions.Insured).AcceptTerms(), Now);
        Assert.Equal(Now, shipment.InsuranceAcceptedUtc);
        shipment.ChangeInstructions(OperationsShipmentInstructions.Pickup(jeremie, "Ana Pierre", "+50937123456", OperationsShipmentOptions.Insured).AcceptTerms(), Now.AddDays(1));
        Assert.Equal(Now.AddDays(1), shipment.InsuranceAcceptedUtc);
        shipment.ChangeInstructions(OperationsShipmentInstructions.Pickup(jeremie, "Ana Pierre", "+50937123456", OperationsShipmentOptions.Insured), Now.AddDays(2));
        Assert.Equal(Now.AddDays(1), shipment.InsuranceAcceptedUtc);
        shipment.ChangeInstructions(OperationsShipmentInstructions.Pickup(jeremie, "Ana Pierre", "+50937123456", OperationsShipmentOptions.None).AcceptTerms(), Now.AddDays(3));
        Assert.Null(shipment.InsuranceAcceptedUtc);
        Assert.Equal(OperationsDestinationErrors.PartnerInvalid, Assert.Throws<DomainException>(() =>
            jeremie.ChangeDestination(new OperationsDestinationTerms(false, null, OperationsDestinationOperator.Partner, " ", null, null, null), Now)).Error);
        Assert.Equal(OperationsDestinationErrors.HubInvalid, Assert.Throws<DomainException>(() =>
            jeremie.ChangeDestination(new OperationsDestinationTerms(false, jeremie.Id, OperationsDestinationOperator.Company, null, null, null, null), Now)).Error);
    }

    [Fact]
    public void InsuranceTerms_HaveIndustryDefaults_AndTheMostPaidPerItemRefusesAValuablePackage()
    {
        OperationsInsurancePlan plan = OperationsInsurancePlan.Create(Tenant, OperationsTransportMode.Ocean, new OperationsInsuranceTerms(OperationsInsuranceMethod.Percent, 2m, null,
            Array.Empty<OperationsInsuranceBand>(), null), null, Now);
        OperationsInsuranceCoverage standard = plan.Versions[0].Coverage;

        Assert.Equal((OperationsInsuranceCover.Own, OperationsInsuranceExclusions.Standard, 7, 21), (standard.Cover, standard.Exclusions, standard.DamageClaimDays, standard.LossClaimDays));
        OperationsInsuranceCoverageTerms insurer = OperationsInsuranceCoverage.Defaults with { Cover = OperationsInsuranceCover.Insurer, InsurerName = "Caribbean Cargo Insurance", PolicyNumber = "CC-1",
            MaxPerItem = 500m, TermsFr = "Conditions de la police." };
        plan.Schedule(new OperationsInsuranceTerms(OperationsInsuranceMethod.Percent, 2m, null, Array.Empty<OperationsInsuranceBand>(), null, insurer), Now.AddDays(1), Now);
        Assert.Equal("Conditions de la police.", plan.Versions[1].Coverage.TermsIn("fr"));
        Assert.Null(plan.Versions[1].Coverage.TermsIn("es"));

        OperationsRateCard ocean = Card(new[] { PerUnit(0, 12m) }, service: "ocean_consolidated");
        Assert.False(RateWith(ocean, new[] { Piece(1m, 10, 10, 10, value: 800m) }, insurance: plan, insured: true, at: Now).InsuranceRefused);
        Assert.True(RateWith(ocean, new[] { Piece(1m, 10, 10, 10, value: 800m) }, insurance: plan, insured: true, at: Now.AddDays(2)).InsuranceRefused);
        Assert.Equal(OperationsChargeErrors.InsuranceInvalid, Assert.Throws<DomainException>(() => OperationsInsuranceCoverage.Create(insurer with { InsurerName = null })).Error);
        Assert.Equal(OperationsChargeErrors.InsuranceInvalid, Assert.Throws<DomainException>(() => OperationsInsuranceCoverage.Create(insurer with { Risks = OperationsInsuredRisks.None })).Error);
    }

    [Fact]
    public void APreferredPrice_LowersTheTotal_WithAReason_NeverBelowGovernmentFees()
    {
        OperationsQuoteLine[] lines =
        {
            new(OperationsChargeLineKind.Freight, null, 1, 10, 10, 2.5m, 25m, false),
            new(OperationsChargeLineKind.Extra, "CONATEL fee", null, 1, null, 10m, 10m, true)
        };
        OperationsQuotePiece[] pieces = { new(4.5m, 10, 10, 10, 100m, null, null) };

        OperationsQuote quote = Quote(pieces, lines, 30m, "Valued customer since 2019");
        Assert.Equal((35m, 30m, true), (quote.StandardTotal, quote.Total, quote.IsPreferred));
        Assert.Equal(OperationsChargeErrors.ReasonRequired, Assert.Throws<DomainException>(() => Quote(pieces, lines, 30m, " ")).Error);
        Assert.Equal(OperationsChargeErrors.PreferredInvalid, Assert.Throws<DomainException>(() => Quote(pieces, lines, 9m, "Too low")).Error);
        Assert.False(Quote(pieces, lines, null, null).IsPreferred);
    }

    private static decimal Premium(OperationsRating rating) => rating.Lines.Single(line => line.Kind == OperationsChargeLineKind.Insurance).Amount;

    private static OperationsQuote Quote(OperationsQuotePiece[] pieces, OperationsQuoteLine[] lines, decimal? preferred, string? reason) =>
        OperationsQuote.Create(Tenant, "QT26000001", Guid.NewGuid(), "air_standard", "HT", false, false, pieces, lines, "USD", preferred, reason, 14, Guid.NewGuid(), Now);

    private static OperationsRateBand Flat(decimal from, decimal amount) => new(from, OperationsBandKind.Flat, amount);

    private static OperationsRateBand PerUnit(decimal from, decimal amount) => new(from, OperationsBandKind.PerUnit, amount);

    private static OperationsRatePrices Prices(params OperationsRateBand[] bands) => new(bands, Array.Empty<OperationsRateItem>(), null, null, null);

    private static OperationsRateCard Card(params OperationsRateBand[] bands) => Card(bands, minimum: null);

    private static OperationsRateCard Card(OperationsRateBand[] bands, string service = "air_standard", Guid? from = null, Guid? to = null, Guid? customer = null,
        OperationsRateItem[]? items = null, decimal? minimum = null, decimal? serviceFee = null, decimal? consolidation = null) =>
        OperationsRateCard.Create(Tenant, service, from, to, customer, OperationsCustomsPolicy.TransportOnly, new OperationsRatePrices(bands, items ?? Array.Empty<OperationsRateItem>(), minimum, serviceFee, consolidation), null, Now);

    private static OperationsChargeExtra Extra(string name, decimal amount, OperationsExtraCalculation calculation, OperationsExtraPer per, OperationsExtraTrigger trigger, string? words = null,
        string? country = null, bool government = false, Guid? to = null, OperationsDeliveryModes delivery = OperationsDeliveryModes.None, OperationsTransportMode? mode = null) =>
        OperationsChargeExtra.Create(Tenant, new OperationsExtraTerms(name, government, trigger, words, null, country, mode, null, to, delivery), amount, calculation, per, null, Now);

    // Pounds and inches, as the company types them.
    private static OperationsRatingPiece Piece(decimal pounds, decimal length = 0, decimal width = 0, decimal height = 0, decimal value = 0, Guid? item = null, string? contents = null) =>
        new(pounds * Kg, length * 2.54m, width * 2.54m, height * 2.54m, value, item, contents);

    private static OperationsRating Rate(OperationsRateCard card, params OperationsRatingPiece[] pieces) => RateWith(card, pieces);

    private static OperationsRating RateWith(OperationsRateCard card, OperationsRatingPiece[] pieces, OperationsChargeExtra[]? extras = null, OperationsInsurancePlan? insurance = null,
        bool consolidated = false, bool insured = false, IReadOnlyDictionary<Guid, int>? quantities = null, Guid? to = null, OperationsDeliveryModes delivery = OperationsDeliveryModes.None,
        DateTimeOffset? at = null, OperationsItemType[]? types = null) =>
        OperationsRatingEngine.Rate(Input(card, pieces, extras, insurance, consolidated, insured, quantities, to, delivery, at, types))!;

    private static OperationsRatingInput Input(OperationsRateCard card, OperationsRatingPiece[] pieces, OperationsChargeExtra[]? extras = null, OperationsInsurancePlan? insurance = null,
        bool consolidated = false, bool insured = false, IReadOnlyDictionary<Guid, int>? quantities = null, Guid? to = null, OperationsDeliveryModes delivery = OperationsDeliveryModes.None,
        DateTimeOffset? at = null, OperationsItemType[]? types = null) =>
        new(card, extras ?? Array.Empty<OperationsChargeExtra>(), insurance, card.ServiceCode.StartsWith("air", StringComparison.Ordinal) ? 6000m : 1000m, OperationsWeightUnit.Pound,
            OperationsDimensionUnit.Inch, "HT", Miami, to ?? Cayes, delivery, pieces, consolidated, insured, at ?? Now, types, quantities);
}
