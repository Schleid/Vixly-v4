using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Tests;

// =========================================================
// FILE: OperationsDispatchDomainTests.cs
//
// PURPOSE:
// Proves the carrier and dispatch rules (Slice 6B, D-1 to D-4).
//
// RESPONSIBILITIES:
// - A carrier's airline prefix, and how its rates are counted
// - A dispatch takes only closed bundles that fit it; a manifested bundle cannot be voided
// - Legs leave and arrive in order, at real times; the first departs it, the last arrives it
// - Cancelled only before it leaves, with a reason
//
// IMPORTANT NOTES:
// - Pure domain; no database
// =========================================================
public sealed class OperationsDispatchDomainTests
{
    private static readonly Guid Tenant = Guid.NewGuid();

    private static readonly Guid Origin = Guid.NewGuid();

    private static readonly Guid Destination = Guid.NewGuid();

    private static readonly Guid User = Guid.NewGuid();

    private static readonly DateTimeOffset Now = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);

    private static OperationsBundle Bundle(Guid? warehouse = null, Guid? destination = null, string service = "air_standard", bool close = true)
    {
        OperationsBundle bundle = OperationsBundle.Create(Tenant, new OperationsBundleDraft(warehouse ?? Origin, destination ?? Destination, "HT", service, null, OperationsBundleBasis.Free, null, null),
            "BD" + Guid.NewGuid().ToString("N")[..8], User, null, Now);

        if (close)
        {
            bundle.Take(1, 2m, 2m, OperationsBundleTargetRule.Warn, Now);
            bundle.Close(3m, 30m, 20m, 10m, User, Now);
        }

        return bundle;
    }

    private static OperationsDispatch Dispatch() =>
        OperationsDispatch.Create(Tenant, new OperationsDispatchDraft(Origin, Destination, "ht", "air_standard", null, null), "DSP26000001", User, null, Now);

    private static OperationsDispatchLegDraft Leg(string from, string to) => new(OperationsCarrierKind.Air, null, from, to, null, null, "123-1", null, null);

    [Fact]
    public void ACarrier_HasAnAirlinePrefixOnlyForAir_AndItsRatesFollowItsKind()
    {
        Assert.Equal(OperationsDispatchErrors.PrefixInvalid, Assert.Throws<DomainException>(() => OperationsCarrier.Create(Tenant, "Ocean Line", OperationsCarrierKind.Ocean, null, "123", Now)).Error);
        Assert.Equal(OperationsDispatchErrors.PrefixInvalid, Assert.Throws<DomainException>(() => OperationsCarrier.Create(Tenant, "Sky", OperationsCarrierKind.Air, null, "12A", Now)).Error);
        Assert.Equal(OperationsDispatchErrors.CarrierInvalid, Assert.Throws<DomainException>(() => OperationsCarrier.Create(Tenant, " ", OperationsCarrierKind.Air, null, null, Now)).Error);

        OperationsCarrier air = OperationsCarrier.Create(Tenant, " Sky Cargo ", OperationsCarrierKind.Air, "ACC", "123", Now);
        Assert.Equal(("Sky Cargo", "123", true), (air.Name, air.AirlinePrefix, air.IsActive));
        OperationsCarrier ocean = OperationsCarrier.Create(Tenant, "Ocean Line", OperationsCarrierKind.Ocean, null, null, Now);
        DateOnly today = DateOnly.FromDateTime(Now.UtcDateTime);

        Assert.Equal(OperationsDispatchErrors.RateInvalid, Assert.Throws<DomainException>(() => OperationsCarrierRate.Create(Tenant, air, null, "HT", null, null, null, 40m, today, Now)).Error);
        Assert.Equal(OperationsDispatchErrors.RateInvalid, Assert.Throws<DomainException>(() => OperationsCarrierRate.Create(Tenant, ocean, null, "HT", null, 2m, null, null, today, Now)).Error);
        Assert.Equal(OperationsDispatchErrors.RateInvalid, Assert.Throws<DomainException>(() => OperationsCarrierRate.Create(Tenant, air, null, "HT", null, 2.00001m, null, null, today, Now)).Error);
        Assert.Equal(OperationsDispatchErrors.DestinationRequired, Assert.Throws<DomainException>(() => OperationsCarrierRate.Create(Tenant, air, null, null, null, 2m, null, null, today, Now)).Error);
        OperationsCarrierRate rate = OperationsCarrierRate.Create(Tenant, air, null, "ht", "Sack", 2.5m, 50m, null, today, Now);
        Assert.Equal(("HT", (string?)null, 2.5m, 50m), (rate.CountryCode, rate.BundleType, rate.PerKg!.Value, rate.Minimum!.Value));
        Assert.Equal(40m, OperationsCarrierRate.Create(Tenant, ocean, null, "HT", null, null, null, 40m, today, Now).PerBundle);
    }

    [Fact]
    public void ADispatch_TakesOnlyClosedBundlesThatFit_AndAManifestedBundleCannotBeVoided()
    {
        OperationsDispatch dispatch = Dispatch();
        Assert.Equal(("HT", OperationsDispatchStatus.Planned), (dispatch.DestinationCountryCode, dispatch.Status));

        Assert.Equal(OperationsDispatchErrors.BundleNotClosed, dispatch.Refuses(Bundle(close: false)));
        Assert.Equal(OperationsDispatchErrors.WrongWarehouse, dispatch.Refuses(Bundle(warehouse: Guid.NewGuid())));
        Assert.Equal(OperationsDispatchErrors.WrongDestination, dispatch.Refuses(Bundle(destination: Guid.NewGuid())));
        Assert.Equal(OperationsDispatchErrors.WrongService, dispatch.Refuses(Bundle(service: "ocean")));

        OperationsBundle bundle = Bundle();
        dispatch.TakeBundle(bundle, Now);
        Assert.Equal((dispatch.Id, 1), (bundle.DispatchId!.Value, dispatch.BundleCount));
        Assert.Equal(OperationsBundleErrors.CannotVoid, Assert.Throws<DomainException>(() => bundle.Void("Wrong", User, Now)).Error);
        Assert.Equal(OperationsDispatchErrors.BundleElsewhere, Assert.Throws<DomainException>(() => Dispatch().TakeBundle(bundle, Now)).Error);

        dispatch.ReleaseBundle(bundle, Now);
        Assert.Equal(((Guid?)null, 0), (bundle.DispatchId, dispatch.BundleCount));
        bundle.Void("Wrong flight", User, Now);
    }

    [Fact]
    public void Legs_LeaveAndArriveInOrder_AtRealTimes_AndTheDispatchFollows()
    {
        OperationsDispatch dispatch = Dispatch();
        Assert.Equal(OperationsDispatchErrors.NothingToClose, Assert.Throws<DomainException>(() => dispatch.Close(Now)).Error);
        dispatch.TakeBundle(Bundle(), Now);
        Assert.Equal(OperationsDispatchErrors.LegTimeInvalid, Assert.Throws<DomainException>(() => dispatch.AddLeg(new OperationsDispatchLegDraft(OperationsCarrierKind.Air, null, "MIA", "SDQ",
            Now, Now.AddHours(-1), null, null, null), Now)).Error);
        Assert.Equal(OperationsDispatchErrors.TextInvalid, Assert.Throws<DomainException>(() => dispatch.AddLeg(Leg("<b>MIA</b>", "SDQ"), Now)).Error);
        OperationsDispatchLeg first = dispatch.AddLeg(Leg("MIA", "SDQ"), Now);
        OperationsDispatchLeg second = dispatch.AddLeg(Leg("SDQ", "PAP"), Now);
        Assert.Equal(OperationsDispatchErrors.NotClosed, Assert.Throws<DomainException>(() => dispatch.DepartLeg(first.Id, Now, Now)).Error);

        dispatch.Close(Now);
        Assert.Equal(OperationsDispatchErrors.NotPlanned, Assert.Throws<DomainException>(() => dispatch.TakeBundle(Bundle(), Now)).Error);
        Assert.Equal(OperationsDispatchErrors.LegOrder, Assert.Throws<DomainException>(() => dispatch.DepartLeg(second.Id, Now, Now)).Error);
        Assert.Equal(OperationsDispatchErrors.LegTimeInvalid, Assert.Throws<DomainException>(() => dispatch.DepartLeg(first.Id, Now.AddHours(1), Now)).Error);

        Assert.True(dispatch.DepartLeg(first.Id, Now.AddHours(-5), Now));
        Assert.Equal((OperationsDispatchStatus.Departed, Now.AddHours(-5)), (dispatch.Status, dispatch.DepartedUtc!.Value));
        Assert.Equal(OperationsDispatchErrors.CannotCancel, Assert.Throws<DomainException>(() => dispatch.Cancel("Late", Now)).Error);
        Assert.Equal(OperationsDispatchErrors.NotPlanned, Assert.Throws<DomainException>(() => dispatch.RemoveLeg(second.Id, Now)).Error);
        Assert.Equal(OperationsDispatchErrors.LegTimeInvalid, Assert.Throws<DomainException>(() => dispatch.ArriveLeg(first.Id, Now.AddHours(-6), Now)).Error);
        Assert.False(dispatch.ArriveLeg(first.Id, Now.AddHours(-3), Now));
        Assert.Equal(OperationsDispatchErrors.LegTimeInvalid, Assert.Throws<DomainException>(() => dispatch.DepartLeg(second.Id, Now.AddHours(-4), Now)).Error);
        Assert.False(dispatch.DepartLeg(second.Id, Now.AddHours(-2), Now));
        Assert.True(dispatch.ArriveLeg(second.Id, Now.AddHours(-1), Now));
        Assert.Equal((OperationsDispatchStatus.Arrived, Now.AddHours(-1)), (dispatch.Status, dispatch.ArrivedUtc!.Value));
    }

    [Fact]
    public void TheCost_UsesTheRateInForce_AtLeastTheMinimum_AndSharesToTheCent()
    {
        OperationsDispatch dispatch = Dispatch();
        OperationsCarrier air = OperationsCarrier.Create(Tenant, "Sky", OperationsCarrierKind.Air, null, "123", Now);
        OperationsCarrier ocean = OperationsCarrier.Create(Tenant, "Sea", OperationsCarrierKind.Ocean, null, null, Now);
        DateOnly today = DateOnly.FromDateTime(Now.UtcDateTime);
        OperationsCarrierRate[] airRates =
        {
            OperationsCarrierRate.Create(Tenant, air, null, "HT", null, 2m, 10m, null, today.AddDays(-10), Now),
            OperationsCarrierRate.Create(Tenant, air, Destination, "HT", null, 3m, 10m, null, today.AddDays(-5), Now),
            OperationsCarrierRate.Create(Tenant, air, Destination, "HT", null, 9m, null, null, today.AddDays(5), Now),
            OperationsCarrierRate.Create(Tenant, air, null, "DO", null, 1m, null, null, today.AddDays(-5), Now)
        };
        OperationsBundle heavy = Bundle();
        OperationsBundle light = Bundle();

        // The warehouse's rate started before today wins over the country's; tomorrow's does not count yet: (3 + 3) kg at 3.00.
        Assert.Equal(3m, OperationsDispatchCost.RateFor(airRates, dispatch, null, today)!.PerKg);
        Assert.Equal(18m, OperationsDispatchCost.LegCost(air, airRates, dispatch, new[] { heavy, light }, today));
        Assert.Equal(10m, OperationsDispatchCost.LegCost(air, airRates, dispatch, new[] { light }, today.AddDays(-7)));
        Assert.Null(OperationsDispatchCost.LegCost(air, airRates, dispatch, new[] { light }, today.AddDays(-20)));

        // Ocean: per bundle at its type's rate, or the rate for any type; a bundle without a rate leaves the cost unknown.
        OperationsCarrierRate[] oceanRates = { OperationsCarrierRate.Create(Tenant, ocean, null, "HT", null, null, null, 40m, today, Now) };
        Assert.Equal(80m, OperationsDispatchCost.LegCost(ocean, oceanRates, dispatch, new[] { heavy, light }, today));
        Assert.Null(OperationsDispatchCost.LegCost(ocean, oceanRates, dispatch, new[] { heavy }, today.AddDays(-1)));

        // Shares by chargeable weight add up to the total; the rounding goes to the largest share.
        Assert.Equal(new[] { 33.33m, 66.67m }, OperationsDispatchCost.Share(100m, new[] { 1m, 2m }));
        Assert.Equal(new[] { 33.34m, 33.33m, 33.33m }, OperationsDispatchCost.Share(100m, new[] { 1m, 1m, 1m }));
        Assert.Equal(new[] { 5m, 5m }, OperationsDispatchCost.Share(10m, new[] { 0m, 0m }));
        IReadOnlyList<decimal> many = OperationsDispatchCost.Share(10.25m, Enumerable.Repeat(1m, 50).ToList());
        Assert.Equal((10.25m, true), (many.Sum(), many.All(share => share is 0.20m or 0.21m)));
        Assert.Equal(50.01m, OperationsDispatchCost.LegCost(air, new[] { OperationsCarrierRate.Create(Tenant, air, null, "HT", null, 0.5m, 50.0050m, null, today, Now) }, dispatch,
            new[] { light }, today));
    }

    [Fact]
    public void TheCost_IsCorrectedWithANote_AndTheLetterOfInstructionIsKeptUntilArrival()
    {
        OperationsDispatch dispatch = Dispatch();
        Assert.Equal(OperationsDispatchErrors.NotClosed, Assert.Throws<DomainException>(() => dispatch.CorrectCost(10m, "Invoice", "USD", Now)).Error);
        dispatch.TakeBundle(Bundle(), Now);
        OperationsCarrier air = OperationsCarrier.Create(Tenant, "Sky", OperationsCarrierKind.Air, null, "123", Now);
        Guid key = Guid.NewGuid();
        OperationsDispatchLeg first = dispatch.AddLeg(new OperationsDispatchLegDraft(OperationsCarrierKind.Air, air.Id, "MIA", "PAP", null, null, "45678901", null, null) { RequestKey = key }, Now);
        Assert.Same(first, dispatch.AddLeg(new OperationsDispatchLegDraft(OperationsCarrierKind.Air, air.Id, "MIA", "PAP", null, null, "45678901", null, null) { RequestKey = key }, Now));
        Assert.Single(dispatch.Legs);
        Assert.Equal("123-45678901", dispatch.MasterWaybill(new Dictionary<Guid, OperationsCarrier> { [air.Id] = air }));
        dispatch.AddLeg(new OperationsDispatchLegDraft(OperationsCarrierKind.Ground, null, "PAP", "Cap-Haïtien", null, null, null, "Paul", null), Now);
        Assert.Equal(("MIA", "PAP", true), (dispatch.AirRoute().From, dispatch.AirRoute().To, dispatch.TravelsByAir));
        dispatch.Close(Now);

        dispatch.Costed(50m, "USD", Now);
        Assert.Equal((50m, 50m), (dispatch.CostComputed!.Value, dispatch.CostTotal!.Value));
        Assert.Equal(OperationsDispatchErrors.ReasonRequired, Assert.Throws<DomainException>(() => dispatch.CorrectCost(75m, " ", "USD", Now)).Error);
        Assert.Equal(OperationsDispatchErrors.CostInvalid, Assert.Throws<DomainException>(() => dispatch.CorrectCost(7.555m, "Invoice", "USD", Now)).Error);
        dispatch.CorrectCost(75m, "Fuel surcharge", "USD", Now);
        dispatch.Costed(55m, "USD", Now);
        Assert.Equal((55m, 75m, "Fuel surcharge"), (dispatch.CostComputed!.Value, dispatch.CostTotal!.Value, dispatch.CostNote));
        dispatch.CorrectCost(null, null, "USD", Now);
        Assert.Equal("USD", dispatch.CostCurrencyCode);
        Assert.Equal((55m, (string?)null), (dispatch.CostTotal!.Value, dispatch.CostNote));

        dispatch.ChangeInstructions(true, " X2026 ", OperationsDispatchDocuments.CommercialInvoice | OperationsDispatchDocuments.PackingList, "Keep dry", Now);
        Assert.Equal((true, "X2026", "Keep dry"), (dispatch.ChargesCollect, dispatch.ExportReference, dispatch.HandlingInstructions));
        Assert.Equal(OperationsDispatchErrors.TextInvalid, Assert.Throws<DomainException>(() => dispatch.ChangeInstructions(false, null, (OperationsDispatchDocuments)64, null, Now)).Error);

        // A typed time over a month old is a typo.
        Assert.Equal(OperationsDispatchErrors.LegTimeInvalid, Assert.Throws<DomainException>(() => dispatch.DepartLeg(first.Id, Now.AddDays(-31), Now)).Error);
        Assert.Equal("DSP26000001-007", OperationsDispatchShipment.House(dispatch.Number, 7));
    }

    [Fact]
    public void ADispatch_IsCancelledBeforeItLeaves_WithAReason_AndHasAtMostSixLegs()
    {
        OperationsDispatch dispatch = Dispatch();
        OperationsBundle bundle = Bundle();
        dispatch.TakeBundle(bundle, Now);

        for (int leg = 0; leg < OperationsDispatch.MaxLegs; leg++)
        {
            dispatch.AddLeg(Leg("A" + leg, "B" + leg), Now);
        }

        Assert.Equal(OperationsDispatchErrors.TooManyLegs, Assert.Throws<DomainException>(() => dispatch.AddLeg(Leg("X", "Y"), Now)).Error);
        dispatch.RemoveLeg(dispatch.Legs.Single(item => item.Sequence == 2).Id, Now);
        Assert.Equal(6, dispatch.AddLeg(Leg("X", "Y"), Now).Sequence);
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6 }, dispatch.Legs.Select(item => item.Sequence).Order().ToArray());
        dispatch.Close(Now);

        foreach (Guid id in dispatch.Legs.Skip(1).Select(item => item.Id).ToList())
        {
            dispatch.RemoveLeg(id, Now);
        }

        Assert.Equal(OperationsDispatchErrors.NothingToClose, Assert.Throws<DomainException>(() => dispatch.RemoveLeg(dispatch.Legs.Single().Id, Now)).Error);
        Assert.Equal(OperationsDispatchErrors.ReasonRequired, Assert.Throws<DomainException>(() => dispatch.Cancel(" ", Now)).Error);
        dispatch.Cancel("Flight cancelled", Now);
        OperationsDispatch.Free(bundle, Now);
        Assert.Equal((OperationsDispatchStatus.Cancelled, 0, (Guid?)null), (dispatch.Status, dispatch.BundleCount, bundle.DispatchId));
        Assert.Equal(OperationsDispatchErrors.DestinationRequired, Assert.Throws<DomainException>(() =>
            OperationsDispatch.Create(Tenant, new OperationsDispatchDraft(Origin, null, null, "air_standard", null, null), "DSP2", User, null, Now)).Error);
    }
}
