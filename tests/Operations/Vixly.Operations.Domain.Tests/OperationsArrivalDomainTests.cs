using Vixly.Operations.Domain.Arrivals;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Tests;

// =========================================================
// FILE: OperationsArrivalDomainTests.cs
//
// PURPOSE:
// Proves the rules of destination receiving (Slice 7 step 1, R-1 to R-3).
//
// RESPONSIBILITIES:
// - A discrepancy: found by itself, resolved as lost or settled with a note, once
// - Customs held blocks the parcel; cleared releases it; the destination milestones
// - A warehouse clears customs or not
//
// IMPORTANT NOTES:
// - Pure domain; no database
// =========================================================
public sealed class OperationsArrivalDomainTests
{
    private static readonly Guid Tenant = Guid.NewGuid();

    private static readonly Guid Parcel = Guid.NewGuid();

    private static readonly DateTimeOffset Now = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ADiscrepancy_IsFoundByItself_OrResolvedWithANote_Once()
    {
        OperationsArrivalDiscrepancy found = OperationsArrivalDiscrepancy.Create(Tenant, Guid.NewGuid(), Guid.NewGuid(), OperationsDiscrepancyKind.MissingParcel, Guid.NewGuid(), Parcel,
            "PKG-1", Now);
        Assert.True(found.IsOpen);
        found.Found(Now.AddHours(1));
        Assert.Equal((OperationsDiscrepancyStatus.Found, Now.AddHours(1)), (found.Status, found.ResolvedUtc!.Value));
        Assert.Equal(OperationsArrivalErrors.NotOpen, Assert.Throws<DomainException>(() => found.Resolve(OperationsDiscrepancyStatus.Lost, "Late", Guid.NewGuid(), Now)).Error);

        OperationsArrivalDiscrepancy lost = OperationsArrivalDiscrepancy.Create(Tenant, null, Guid.NewGuid(), OperationsDiscrepancyKind.ExtraBundle, Guid.NewGuid(), null, new string('B', 80), Now);
        Assert.Equal(OperationsArrivalDiscrepancy.CodeMaxLength, lost.Code.Length);
        Assert.Equal(OperationsArrivalErrors.ReasonRequired, Assert.Throws<DomainException>(() => lost.Resolve(OperationsDiscrepancyStatus.Found, "x", Guid.NewGuid(), Now)).Error);
        Assert.Equal(OperationsArrivalErrors.ReasonRequired, Assert.Throws<DomainException>(() => lost.Resolve(OperationsDiscrepancyStatus.Lost, " ", Guid.NewGuid(), Now)).Error);
        Assert.Equal(OperationsArrivalErrors.ReasonRequired, Assert.Throws<DomainException>(() => lost.Resolve(OperationsDiscrepancyStatus.Lost, "<b>", Guid.NewGuid(), Now)).Error);
        Guid user = Guid.NewGuid();
        lost.Resolve(OperationsDiscrepancyStatus.Lost, "  Not on the truck ", user, Now);
        Assert.Equal((OperationsDiscrepancyStatus.Lost, "Not on the truck", user), (lost.Status, lost.Note, lost.ResolvedByUserId!.Value));
        lost.Found(Now.AddDays(1));
        Assert.Equal(OperationsDiscrepancyStatus.Lost, lost.Status);
        Assert.Throws<ArgumentOutOfRangeException>(() => OperationsArrivalDiscrepancy.Create(Tenant, null, Guid.NewGuid(), (OperationsDiscrepancyKind)9, null, null, "X", Now));
    }

    [Fact]
    public void Customs_HeldBlocksTheParcel_ClearedReleasesIt_AndTheDestinationMilestonesFollow()
    {
        OperationsTrackingEvent arrived = Record(OperationsTrackingCodes.ArrivedDestinationWarehouse, new() { ["place"] = "Capois" }, Now);
        OperationsTrackingEvent held = Record(OperationsTrackingCodes.CustomsHeld, new() { ["message"] = "Invoice needed" }, Now.AddMinutes(1));
        OperationsTrackingEvent cleared = Record(OperationsTrackingCodes.CustomsCleared, new(), Now.AddMinutes(2));
        OperationsTrackingEvent ready = Record(OperationsTrackingCodes.ReadyForPickup, new() { ["place"] = "Capois" }, Now.AddMinutes(3));

        OperationsParcelState waiting = OperationsTrackingProjection.Replay(new[] { arrived, held });
        Assert.True(waiting.Flags.HasFlag(OperationsParcelFlags.CustomsHeld));
        Assert.Equal(OperationsMilestone.ArrivedDestinationWarehouse, waiting.Milestone);
        OperationsParcelState released = OperationsTrackingProjection.Replay(new[] { arrived, held, cleared, ready });
        Assert.False(released.Flags.HasFlag(OperationsParcelFlags.CustomsHeld));
        Assert.Equal(OperationsMilestone.ReadyForPickup, released.Milestone);

        // What the customer sees: the place, the customs line; the bundle and dispatch steps are internal.
        Assert.Equal(new[] { "place" }, OperationsTrackingCodes.Find(OperationsTrackingCodes.ReadyForPickup)!.CustomerArguments);
        Assert.Equal(new[] { "message" }, OperationsTrackingCodes.Find(OperationsTrackingCodes.CustomsHeld)!.CustomerArguments);
        Assert.Equal(OperationsTrackingVisibility.Customer, OperationsTrackingCodes.Find(OperationsTrackingCodes.InCustoms)!.Visibility);

        foreach (string code in new[] { OperationsTrackingCodes.BundleArrived, OperationsTrackingCodes.BundleOpened, OperationsTrackingCodes.DispatchReceived })
        {
            Assert.Equal(OperationsTrackingVisibility.Internal, OperationsTrackingCodes.Find(code)!.Visibility);
        }

        Assert.Contains("missing_in_transit", Vixly.Operations.Domain.Events.OperationsEventReasons.Exception);
    }

    [Fact]
    public void AWarehouse_ClearsCustoms_AsItsDestinationTermsSay()
    {
        OperationsWarehouse warehouse = OperationsWarehouse.Create(Tenant, Guid.NewGuid(), new OperationsLocationDetails("PAP1", "Capois",
            OperationsPostalAddress.Create("12 Rue Capois", null, "Port-au-Prince", null, null, "HT"), null, null, null, true), OperationsWarehouseRoles.PickupPoint, false, Now);
        Assert.False(warehouse.ClearsCustoms);
        warehouse.ChangeDestination(new OperationsDestinationTerms(false, null, OperationsDestinationOperator.Company, null, null, null, null) { ClearsCustoms = true }, Now);
        Assert.True(warehouse.ClearsCustoms);
    }

    private static OperationsTrackingEvent Record(string code, Dictionary<string, string> arguments, DateTimeOffset at) =>
        OperationsTrackingEvent.Record(Tenant, new OperationsTrackingDraft(OperationsTrackingSubject.Parcel, Parcel, code, arguments, at, "test:" + Guid.NewGuid().ToString("N"),
            OperationsTrackingActor.Staff, OperationsTrackingSource.Scan) { ActorUserId = Guid.NewGuid() }, Now.AddMinutes(10));
}
