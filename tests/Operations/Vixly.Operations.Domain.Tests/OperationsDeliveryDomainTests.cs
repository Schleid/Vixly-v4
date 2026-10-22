using Vixly.Operations.Domain.Delivery;
using Vixly.Operations.Domain.Pickup;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Tests;

// =========================================================
// FILE: OperationsDeliveryDomainTests.cs
//
// PURPOSE:
// Proves the rules of home delivery (Slice 7 step 3, R-7, R-12 to R-15).
//
// RESPONSIBILITIES:
// - A route: planned with a driver and a day, started with stops, back when every stop is done, settled with the cash counted
// - A stop: delivered with the receiver's name and the ID's last four characters only, or failed with a known reason
// - The company's delivery settings; a failed delivery steps the parcel back; going back and returned
//
// IMPORTANT NOTES:
// - Pure domain; no database
// =========================================================
public sealed class OperationsDeliveryDomainTests
{
    private static readonly Guid Tenant = Guid.NewGuid();

    private static readonly Guid Parcel = Guid.NewGuid();

    private static readonly DateTimeOffset Now = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);

    private static readonly DateOnly Today = new(2026, 10, 21);

    [Fact]
    public void ARoute_IsPlanned_Started_Back_ThenSettled_WithTheCashCounted()
    {
        Assert.Equal(OperationsDeliveryErrors.DriverRequired, Assert.Throws<DomainException>(() => Plan(" ")).Error);
        Assert.Equal(OperationsDeliveryErrors.DriverRequired, Assert.Throws<DomainException>(() => Plan("<b>Paul</b>")).Error);
        Assert.Equal(OperationsDeliveryErrors.DateInvalid, Assert.Throws<DomainException>(() => Plan("Paul", Today.AddDays(31))).Error);
        Assert.Equal(OperationsDeliveryErrors.DateInvalid, Assert.Throws<DomainException>(() => Plan("Paul", Today.AddDays(-2))).Error);

        OperationsDeliveryRoute route = Plan("  Paul Driver ");
        Assert.Equal(("Paul Driver", OperationsRouteStatus.Planned), (route.DriverName, route.Status));
        Assert.Equal(OperationsDeliveryErrors.Empty, Assert.Throws<DomainException>(() => route.Start(Now)).Error);
        Assert.Equal(OperationsDeliveryErrors.TooManyStops, Assert.Throws<DomainException>(() => route.StopsChanged(OperationsDeliveryRoute.MaxStops + 1, Now)).Error);
        route.StopsChanged(2, Now);
        Assert.Equal(OperationsDeliveryErrors.NotOut, Assert.Throws<DomainException>(() => route.StopDone(0m, false, Now)).Error);
        route.Start(Now);
        Assert.Equal(OperationsDeliveryErrors.NotPlanned, Assert.Throws<DomainException>(() => route.StopsChanged(1, Now)).Error);
        Assert.Equal(OperationsDeliveryErrors.NotPlanned, Assert.Throws<DomainException>(() => route.Cancel("late", Now)).Error);
        Assert.Equal(OperationsDeliveryErrors.NotReturned, Assert.Throws<DomainException>(() => route.Settle(0m, null, Guid.NewGuid(), Now)).Error);

        route.StopDone(25.50m, false, Now);
        Assert.Equal(OperationsRouteStatus.Out, route.Status);
        route.StopDone(0m, true, Now);
        Assert.Equal((OperationsRouteStatus.Returned, 25.50m), (route.Status, route.CashCollected));

        // A difference needs a note; the amount is in cents.
        Assert.Equal(OperationsDeliveryErrors.CashInvalid, Assert.Throws<DomainException>(() => route.Settle(25m, null, Guid.NewGuid(), Now)).Error);
        Assert.Equal(OperationsDeliveryErrors.CashInvalid, Assert.Throws<DomainException>(() => route.Settle(25.505m, "x", Guid.NewGuid(), Now)).Error);
        Assert.Equal(OperationsDeliveryErrors.CashInvalid, Assert.Throws<DomainException>(() => route.Settle(-1m, "x", Guid.NewGuid(), Now)).Error);
        route.Settle(25m, " Short 0.50, change given ", Guid.NewGuid(), Now);
        Assert.Equal((OperationsRouteStatus.Settled, 25m, "Short 0.50, change given"), (route.Status, route.CashHandedIn!.Value, route.SettleNote));

        OperationsDeliveryRoute other = Plan("Paul");
        Assert.Equal(OperationsDeliveryErrors.ReasonRequired, Assert.Throws<DomainException>(() => other.Cancel(" ", Now)).Error);
        other.Cancel("Truck broke down", Now);
        Assert.Equal(OperationsRouteStatus.Cancelled, other.Status);
    }

    [Fact]
    public void AStop_IsDelivered_WithTheReceiversName_OrFailed_WithAKnownReason_Once()
    {
        OperationsDeliveryStop stop = OperationsDeliveryStop.Create(Tenant, Guid.NewGuid(), Guid.NewGuid(), 1, 2, Now);
        Guid user = Guid.NewGuid();
        Assert.Equal(OperationsDeliveryErrors.ReceiverRequired, Assert.Throws<DomainException>(() => stop.Deliver(" ", null, null, null, null, false, false, 0m, user, Now)).Error);
        Assert.Equal(OperationsPickupErrors.IdRequired, Assert.Throws<DomainException>(() => stop.Deliver("Jean", OperationsPickupIdKind.Passport, "123456789", null, null, false, false, 0m,
            user, Now)).Error);
        Assert.Equal(OperationsPickupErrors.IdRequired, Assert.Throws<DomainException>(() => stop.Deliver("Jean", null, "1234", null, null, false, false, 0m, user, Now)).Error);
        Assert.Equal(OperationsDeliveryErrors.CashInvalid, Assert.Throws<DomainException>(() => stop.Deliver("Jean", null, null, null, null, false, false, 1.234m, user, Now)).Error);

        Guid invoice = Guid.NewGuid();
        stop.Deliver(" Jean Baptiste ", OperationsPickupIdKind.NationalId, "ab12", "deliveries/x.png", null, true, false, 30m, user, Now, new[] { invoice });
        Assert.Equal(new[] { invoice }, stop.CollectedInvoiceIds);
        Assert.Equal(("Jean Baptiste", "AB12", 30m, OperationsStopStatus.Delivered, true), (stop.ReceiverName, stop.IdLast, stop.CashCollected, stop.Status, stop.IsDone));
        Assert.Equal(OperationsDeliveryErrors.StopDone, Assert.Throws<DomainException>(() => stop.Fail("not_home", null, user, Now)).Error);

        OperationsDeliveryStop failed = OperationsDeliveryStop.Create(Tenant, Guid.NewGuid(), Guid.NewGuid(), 2, 1, Now);
        Assert.Equal(OperationsDeliveryErrors.ReasonRequired, Assert.Throws<DomainException>(() => failed.Fail("lost_keys", null, user, Now)).Error);
        Assert.Equal(OperationsDeliveryErrors.ReasonRequired, Assert.Throws<DomainException>(() => failed.Fail("other", " ", user, Now)).Error);
        Assert.Equal(OperationsDeliveryErrors.ReasonRequired, Assert.Throws<DomainException>(() => failed.Fail("not_home", "<script>", user, Now)).Error);
        failed.Fail("not_home", " Rang twice ", user, Now);
        Assert.Empty(failed.CollectedInvoiceIds);
        Assert.Equal(("not_home", "Rang twice", OperationsStopStatus.Failed), (failed.FailureReason, failed.Note, failed.Status));
    }

    [Fact]
    public void TheDeliverySettings_AreTheCompanys_WithSafeDefaults()
    {
        OperationsTenantSettings settings = OperationsTenantSettings.CreateDefault(Tenant, Now);
        Assert.Equal((OperationsDeliveryRecording.Phone, false, 3, OperationsAfterFailedDelivery.WaitForPickup, OperationsDeliveryProofs.Signature, true),
            (settings.DeliveryRecording, settings.CashOnDelivery, settings.DeliveryTries, settings.AfterFailedDelivery, settings.DeliveryProofs, settings.DeliveryEmails));
        Assert.Equal("RT", settings.Prefix(OperationsNumberKind.Route));

        foreach ((OperationsDeliveryRecording recording, int tries, OperationsAfterFailedDelivery after, OperationsDeliveryProofs proofs) in new[]
        {
            ((OperationsDeliveryRecording)9, 3, OperationsAfterFailedDelivery.WaitForPickup, OperationsDeliveryProofs.Default),
            (OperationsDeliveryRecording.Sheet, 0, OperationsAfterFailedDelivery.WaitForPickup, OperationsDeliveryProofs.Default),
            (OperationsDeliveryRecording.Sheet, 11, OperationsAfterFailedDelivery.WaitForPickup, OperationsDeliveryProofs.Default),
            (OperationsDeliveryRecording.Sheet, 3, (OperationsAfterFailedDelivery)7, OperationsDeliveryProofs.Default),
            (OperationsDeliveryRecording.Sheet, 3, OperationsAfterFailedDelivery.WaitForPickup, (OperationsDeliveryProofs)64)
        })
        {
            Assert.Equal(OperationsDeliveryErrors.SettingsInvalid, Assert.Throws<DomainException>(() => settings.ChangeDelivery(recording, true, tries, after, proofs, true, Now)).Error);
        }

        settings.ChangeDelivery(OperationsDeliveryRecording.Sheet, true, 2, OperationsAfterFailedDelivery.ReturnToOrigin, OperationsDeliveryProofs.Id | OperationsDeliveryProofs.Code, false, Now);
        Assert.Equal((OperationsDeliveryRecording.Sheet, true, 2, OperationsAfterFailedDelivery.ReturnToOrigin, OperationsDeliveryProofs.Id | OperationsDeliveryProofs.Code, false),
            (settings.DeliveryRecording, settings.CashOnDelivery, settings.DeliveryTries, settings.AfterFailedDelivery, settings.DeliveryProofs, settings.DeliveryEmails));
    }

    [Fact]
    public void AFailedDelivery_StepsTheParcelBack_AndADeliveredParcelStaysDelivered()
    {
        OperationsTrackingEvent arrived = Record(OperationsTrackingCodes.ArrivedDestinationWarehouse, new() { ["place"] = "Capois" }, Now);
        OperationsTrackingEvent out1 = Record(OperationsTrackingCodes.OutForDelivery, new(), Now.AddMinutes(1));
        OperationsTrackingEvent failed = Record(OperationsTrackingCodes.DeliveryFailed, new() { ["failure"] = "not_home" }, Now.AddMinutes(2));
        OperationsTrackingEvent ready = Record(OperationsTrackingCodes.ReadyForPickup, new() { ["place"] = "Capois" }, Now.AddMinutes(3));
        OperationsTrackingEvent out2 = Record(OperationsTrackingCodes.OutForDelivery, new(), Now.AddMinutes(4));
        OperationsTrackingEvent delivered = Record(OperationsTrackingCodes.Delivered, new(), Now.AddMinutes(5));
        OperationsTrackingEvent late = Record(OperationsTrackingCodes.DeliveryFailed, new() { ["failure"] = "refused" }, Now.AddMinutes(6));

        Assert.Equal(OperationsMilestone.OutForDelivery, OperationsTrackingProjection.Replay(new[] { arrived, out1 }).Milestone);
        Assert.Equal(OperationsMilestone.ArrivedDestinationWarehouse, OperationsTrackingProjection.Replay(new[] { arrived, out1, failed }).Milestone);
        Assert.Equal(OperationsMilestone.ReadyForPickup, OperationsTrackingProjection.Replay(new[] { arrived, out1, failed, ready }).Milestone);
        Assert.Equal(OperationsMilestone.Delivered, OperationsTrackingProjection.Replay(new[] { arrived, out1, failed, out2, delivered, late }).Milestone);

        // An unknown reason is refused; the customer sees the reason.
        Assert.Throws<DomainException>(() => Record(OperationsTrackingCodes.DeliveryFailed, new() { ["failure"] = "lost_keys" }, Now));
        Assert.Equal(new[] { "failure" }, OperationsTrackingCodes.Find(OperationsTrackingCodes.DeliveryFailed)!.CustomerArguments);

        // Going back to the sender: marked until it is returned.
        OperationsTrackingEvent going = Record(OperationsTrackingCodes.DeliveryReturning, new(), Now.AddMinutes(3));
        OperationsTrackingEvent returned = Record(OperationsTrackingCodes.ReturnedToSender, new(), Now.AddMinutes(4));
        Assert.True(OperationsTrackingProjection.Replay(new[] { arrived, out1, failed, going }).Flags.HasFlag(OperationsParcelFlags.ReturnPending));
        OperationsParcelState back = OperationsTrackingProjection.Replay(new[] { arrived, out1, failed, going, returned });
        Assert.False(back.Flags.HasFlag(OperationsParcelFlags.ReturnPending));
        Assert.Equal(OperationsMilestone.ReturnedToSender, back.Milestone);
    }

    [Fact]
    public void AParcel_IsOnOneRouteAtATime_AndEachFailureCountsATry()
    {
        OperationsParcel parcel = OperationsParcel.Create(Tenant, null, Now);
        Guid route = Guid.NewGuid();
        parcel.PutOnRoute(route, Now);
        Assert.Equal(OperationsDeliveryErrors.OnAnotherRoute, Assert.Throws<DomainException>(() => parcel.PutOnRoute(Guid.NewGuid(), Now)).Error);
        parcel.OffRoute(true, Now);
        Assert.Equal((null, 1), (parcel.RouteId, parcel.DeliveryTries));
        parcel.PutOnRoute(route, Now);
        parcel.OffRoute(false, Now);
        Assert.Equal((null, 1), (parcel.RouteId, parcel.DeliveryTries));
    }

    private static OperationsDeliveryRoute Plan(string driver, DateOnly? day = null) =>
        OperationsDeliveryRoute.Plan(Tenant, "RT26000001", Guid.NewGuid(), day ?? Today, null, driver, Guid.NewGuid(), null, Today, Now);

    private static OperationsTrackingEvent Record(string code, Dictionary<string, string> arguments, DateTimeOffset at) =>
        OperationsTrackingEvent.Record(Tenant, new OperationsTrackingDraft(OperationsTrackingSubject.Parcel, Parcel, code, arguments, at, "test:" + Guid.NewGuid().ToString("N"),
            OperationsTrackingActor.Staff, OperationsTrackingSource.Manual) { ActorUserId = Guid.NewGuid() }, Now.AddMinutes(10));
}
