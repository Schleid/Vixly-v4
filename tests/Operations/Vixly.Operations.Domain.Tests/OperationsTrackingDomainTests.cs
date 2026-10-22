using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Tests;

// =========================================================
// FILE: OperationsTrackingDomainTests.cs
//
// PURPOSE:
// Proves the tracking event rules of Slice 3A (ADR-0018, ADR-0021).
//
// RESPONSIBILITIES:
// - The catalogue is well formed; events check code, arguments, key and time
// - The projection follows the milestone order, flags, holds and voids
// - The arrival order of events never changes the result (offline scans)
//
// IMPORTANT NOTES:
// - Pure domain, no database
// =========================================================
public sealed class OperationsTrackingDomainTests
{
    private static readonly Guid Tenant = Guid.NewGuid();

    private static readonly Guid Parcel = Guid.NewGuid();

    private static readonly DateTimeOffset Now = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Catalogue_NamesAreUniqueAndFollowThePattern()
    {
        Assert.Equal(OperationsTrackingCodes.All.Count, OperationsTrackingCodes.All.Select(code => code.Name).Distinct().Count());
        Assert.All(OperationsTrackingCodes.All, code => Assert.Matches("^ops\\.tracking\\.[a-z_]+$", code.Name));
        Assert.All(OperationsTrackingCodes.All.Where(code => code.Milestone is not null), code => Assert.Equal(OperationsTrackingVisibility.Customer, code.Visibility));
    }

    [Theory]
    [InlineData("ops.tracking.nothing", "ops.tracking.code_unknown")]
    [InlineData(OperationsTrackingCodes.ReceivedAtOrigin, "ops.tracking.argument_missing")]
    public void UnknownCodes_AndMissingArguments_AreRefused(string code, string error)
    {
        Assert.Equal(error, Assert.Throws<DomainException>(() => Record(code, new())).Error.Code);
    }

    [Theory]
    [InlineData("extra", "x", "ops.tracking.argument_unknown")]
    [InlineData("reason", "Nonsense", "ops.tracking.argument_invalid")]
    [InlineData("reason", "None", "ops.tracking.argument_invalid")]
    [InlineData("reason", "64", "ops.tracking.argument_invalid")]
    [InlineData("reason", "Damaged, Refused", "ops.tracking.argument_invalid")]
    public void HoldArguments_AreChecked(string name, string value, string error)
    {
        Dictionary<string, string> arguments = new() { ["reason"] = "Damaged" };
        arguments[name] = value;

        Assert.Equal(error, Assert.Throws<DomainException>(() => Record(OperationsTrackingCodes.HoldOpened, arguments)).Error.Code);
    }

    [Fact]
    public void Key_Time_AndVoidTarget_AreChecked()
    {
        Assert.Equal("ops.tracking.idempotency_key_invalid", Assert.Throws<DomainException>(() => Record(OperationsTrackingCodes.PreAlerted, new(), key: "short")).Error.Code);
        Assert.Equal("ops.tracking.time_invalid", Assert.Throws<DomainException>(() => Record(OperationsTrackingCodes.PreAlerted, new(), at: Now.AddMinutes(6))).Error.Code);
        Assert.Equal("ops.tracking.void_target_required", Assert.Throws<DomainException>(() => Record(OperationsTrackingCodes.EventVoided, new() { ["why"] = "duplicate" })).Error.Code);
        Assert.Equal("ops.tracking.void_target_required", Assert.Throws<DomainException>(() => Record(OperationsTrackingCodes.PreAlerted, new(), voids: Guid.NewGuid())).Error.Code);

        // A device clock up to five minutes fast is accepted.
        Assert.Equal(Now.AddMinutes(4), Record(OperationsTrackingCodes.PreAlerted, new(), at: Now.AddMinutes(4)).OccurredUtc);
    }

    [Fact]
    public void ControlledValues_Actor_Location_AndTimeZone_AreChecked()
    {
        Assert.Equal("ops.tracking.argument_invalid", Assert.Throws<DomainException>(() =>
            Record(OperationsTrackingCodes.InspectionCompleted, new() { ["result"] = "looks undervalued, call the boss" })).Error.Code);
        Assert.Equal("ops.tracking.argument_missing", Assert.Throws<DomainException>(() =>
            Record(OperationsTrackingCodes.EventVoided, new(), voids: Guid.NewGuid())).Error.Code);

        OperationsTrackingDraft draft = new(OperationsTrackingSubject.Parcel, Parcel, OperationsTrackingCodes.PreAlerted, new Dictionary<string, string>(), Now, "test:actor-0001",
            OperationsTrackingActor.Staff, OperationsTrackingSource.Manual);

        Assert.Equal("ops.tracking.actor_invalid", Assert.Throws<DomainException>(() => OperationsTrackingEvent.Record(Tenant, draft, Now)).Error.Code);
        Assert.Equal("ops.tracking.actor_invalid", Assert.Throws<DomainException>(() =>
            OperationsTrackingEvent.Record(Tenant, draft with { Actor = (OperationsTrackingActor)99, ActorUserId = Guid.NewGuid() }, Now)).Error.Code);
        Assert.Equal("ops.tracking.location_invalid", Assert.Throws<DomainException>(() =>
            OperationsTrackingEvent.Record(Tenant, draft with { ActorUserId = Guid.NewGuid(), LocationCode = "A 03/2" }, Now)).Error.Code);
        Assert.Equal("ops.tracking.time_invalid", Assert.Throws<DomainException>(() =>
            OperationsTrackingEvent.Record(Tenant, draft with { ActorUserId = Guid.NewGuid(), TimeZone = "Mars/Olympus_Mons" }, Now)).Error.Code);

        OperationsTrackingEvent ok = OperationsTrackingEvent.Record(Tenant, draft with
        {
            ActorUserId = Guid.NewGuid(), TimeZone = "America/Port-au-Prince", LocationCode = "MIA-A-03", OccurredUtc = new DateTimeOffset(2026, 10, 18, 8, 0, 0, TimeSpan.FromHours(-4))
        }, Now);
        Assert.Equal(TimeSpan.Zero, ok.OccurredUtc.Offset);
        Assert.Equal(12, ok.OccurredUtc.Hour);
    }

    [Fact]
    public void Milestones_MoveForwardOnly_AndTerminalIsFinal()
    {
        OperationsTrackingEvent received = Received(Now);
        OperationsTrackingEvent latePreAlert = Record(OperationsTrackingCodes.PreAlerted, new(), at: Now.AddMinutes(1));

        Assert.Equal(OperationsMilestone.ReceivedAtOrigin, OperationsTrackingProjection.Replay(new[] { received, latePreAlert }).Milestone);

        OperationsTrackingEvent disposed = Record(OperationsTrackingCodes.Disposed, new(), at: Now.AddMinutes(2));
        OperationsTrackingEvent again = Received(Now.AddMinutes(3));

        Assert.Equal(OperationsMilestone.Disposed, OperationsTrackingProjection.Replay(new[] { received, disposed, again }).Milestone);
        Assert.Null(OperationsTrackingProjection.Replay(Array.Empty<OperationsTrackingEvent>()).Milestone);
    }

    [Fact]
    public void Holds_AreCounted_ByReason_AndBlock()
    {
        OperationsTrackingEvent damaged = Hold(OperationsTrackingCodes.HoldOpened, "Damaged", Now);
        OperationsTrackingEvent unidentified = Hold(OperationsTrackingCodes.HoldOpened, "Unidentified", Now.AddMinutes(1));
        OperationsTrackingEvent damagedReleased = Hold(OperationsTrackingCodes.HoldReleased, "Damaged", Now.AddMinutes(2));
        OperationsTrackingEvent unidentifiedReleased = Hold(OperationsTrackingCodes.HoldReleased, "Unidentified", Now.AddMinutes(3));

        OperationsParcelState both = OperationsTrackingProjection.Replay(new[] { damaged, unidentified });
        OperationsParcelState one = OperationsTrackingProjection.Replay(new[] { damaged, unidentified, damagedReleased });
        OperationsParcelState none = OperationsTrackingProjection.Replay(new[] { damaged, unidentified, damagedReleased, unidentifiedReleased });

        Assert.Equal(OperationsHoldReasons.Damaged | OperationsHoldReasons.Unidentified, both.HoldReasons);
        Assert.True(both.IsBlocked);
        Assert.Equal(OperationsHoldReasons.Unidentified, one.HoldReasons);
        Assert.True(one.Flags.HasFlag(OperationsParcelFlags.OnHold));
        Assert.Equal(OperationsHoldReasons.None, none.HoldReasons);
        Assert.False(none.Flags.HasFlag(OperationsParcelFlags.OnHold));
        Assert.False(none.IsBlocked);
    }

    [Fact]
    public void InspectionFlags_FollowTheSteps()
    {
        OperationsTrackingEvent required = Record(OperationsTrackingCodes.InspectionRequired, new() { ["why"] = "random" }, at: Now);
        OperationsTrackingEvent started = Record(OperationsTrackingCodes.InspectionStarted, new(), at: Now.AddMinutes(1));
        OperationsTrackingEvent completed = Record(OperationsTrackingCodes.InspectionCompleted, new() { ["result"] = "passed" }, at: Now.AddMinutes(2));

        Assert.Equal(OperationsParcelFlags.InspectionPending, OperationsTrackingProjection.Replay(new[] { required }).Flags);
        Assert.Equal(OperationsParcelFlags.InspectionInProgress, OperationsTrackingProjection.Replay(new[] { required, started }).Flags);
        Assert.Equal(OperationsParcelFlags.None, OperationsTrackingProjection.Replay(new[] { required, started, completed }).Flags);
    }

    [Fact]
    public void AVoidedEvent_NoLongerCounts()
    {
        OperationsTrackingEvent hold = Hold(OperationsTrackingCodes.HoldOpened, "Damaged", Now);
        OperationsTrackingEvent voids = Record(OperationsTrackingCodes.EventVoided, new() { ["why"] = "wrong_parcel" }, at: Now.AddMinutes(1), voids: hold.Id);

        Assert.Equal(OperationsHoldReasons.None, OperationsTrackingProjection.Replay(new[] { hold, voids }).HoldReasons);
    }

    [Fact]
    public void ArrivalOrder_NeverChangesTheState()
    {
        OperationsTrackingEvent[] events =
        {
            Received(Now),
            Hold(OperationsTrackingCodes.HoldOpened, "Damaged", Now.AddMinutes(1)),
            Record(OperationsTrackingCodes.InspectionRequired, new() { ["why"] = "rule" }, at: Now.AddMinutes(2)),
            Hold(OperationsTrackingCodes.HoldReleased, "Damaged", Now.AddMinutes(3)),
            Record(OperationsTrackingCodes.InspectionStarted, new(), at: Now.AddMinutes(4))
        };
        OperationsParcelState expected = OperationsTrackingProjection.Replay(events);
        Random random = new(42);

        for (int round = 0; round < 50; round++)
        {
            Assert.Equal(expected, OperationsTrackingProjection.Replay(events.OrderBy(_ => random.Next()).ToArray()));
        }

        Assert.Equal(OperationsMilestone.ReceivedAtOrigin, expected.Milestone);
        Assert.Equal(OperationsParcelFlags.InspectionInProgress | OperationsParcelFlags.AwaitingCheck, expected.Flags);
        Assert.Equal(Now.AddMinutes(4), expected.LastOccurredUtc);
    }

    [Fact]
    public void Check_ClearsTheWaitForTheInspectionStation_InAnyArrivalOrder()
    {
        OperationsTrackingEvent[] events =
        {
            Received(Now.AddMinutes(-10)),
            Record(OperationsTrackingCodes.Checked, new() { ["weight"] = "2.5", ["size"] = "30 x 20 x 10" }, at: Now)
        };

        Assert.Equal(OperationsParcelFlags.AwaitingCheck, OperationsTrackingProjection.Replay(events.Take(1).ToArray()).Flags);
        Assert.Equal(OperationsParcelFlags.None, OperationsTrackingProjection.Replay(events).Flags);
        Assert.Equal(OperationsParcelFlags.None, OperationsTrackingProjection.Replay(events.Reverse().ToArray()).Flags);
    }

    private static OperationsTrackingEvent Received(DateTimeOffset at) => Record(OperationsTrackingCodes.ReceivedAtOrigin, new() { ["warehouse"] = "MIA" }, at: at);

    private static OperationsTrackingEvent Hold(string code, string reason, DateTimeOffset at) => Record(code, new() { ["reason"] = reason }, at: at);

    private static OperationsTrackingEvent Record(string code, Dictionary<string, string> arguments, DateTimeOffset? at = null, string? key = null, Guid? voids = null) =>
        OperationsTrackingEvent.Record(Tenant, new OperationsTrackingDraft(OperationsTrackingSubject.Parcel, Parcel, code, arguments, at ?? Now, key ?? "test:" + Guid.NewGuid().ToString("N"),
            OperationsTrackingActor.Staff, OperationsTrackingSource.Manual) { VoidsEventId = voids, ActorUserId = Guid.NewGuid() }, Now);
}
