using Vixly.Operations.Application.Tests.Fakes;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.Tests;

// =========================================================
// FILE: OperationsTrackerTests.cs
//
// PURPOSE:
// Proves the tracking service of Slice 3A (ADR-0021 decisions 1 to 5).
//
// RESPONSIBILITIES:
// - A repeat returns the first event; a reused key for something else is refused
// - The package state is re-projected with every event, voids included
// - Staff, customer and public views show what each may see
//
// IMPORTANT NOTES:
// - In-memory store; the database version is in the infrastructure tests
// =========================================================
public sealed class OperationsTrackerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeTrackingStore _store = new();

    private readonly FakeTenantScope _scope = new();

    private readonly OperationsTracker _tracker;

    private readonly OperationsParcel _package;

    public OperationsTrackerTests()
    {
        Guid tenant = Guid.NewGuid();
        _scope.Enter(tenant);
        _tracker = new OperationsTracker(_store, _scope, new FixedTimeProvider(Now));
        _package = OperationsParcel.Create(tenant, null, Now);
        _store.Add(_package);
    }

    [Fact]
    public async Task ARepeat_ReturnsTheFirstEvent_AndAddsNothing()
    {
        OperationsTrackingResult first = await _tracker.RecordAsync(Draft(OperationsTrackingCodes.ReceivedAtOrigin, "scan:0001", ("warehouse", "MIA")), CancellationToken.None);
        OperationsTrackingResult again = await _tracker.RecordAsync(Draft(OperationsTrackingCodes.ReceivedAtOrigin, "scan:0001", ("warehouse", "MIA")), CancellationToken.None);
        OperationsTrackingResult other = await _tracker.RecordAsync(Draft(OperationsTrackingCodes.PutAway, "scan:0001", ("location", "A-1")), CancellationToken.None);

        Assert.True(first.Created);
        Assert.False(again.Created);
        Assert.Same(first.Event, again.Event);
        Assert.Equal("ops.tracking.idempotency_key_reused", other.Error?.Code);
        Assert.Single(_store.Events);
        Assert.Equal(OperationsMilestone.ReceivedAtOrigin, _package.Milestone);
    }

    [Fact]
    public async Task ThePackage_FollowsItsEvents_AndAVoid()
    {
        await _tracker.RecordAsync(Draft(OperationsTrackingCodes.ReceivedAtOrigin, "scan:0002", ("warehouse", "MIA")), CancellationToken.None);
        OperationsTrackingResult hold = await _tracker.RecordAsync(Draft(OperationsTrackingCodes.HoldOpened, "cmd:hold-0001", ("reason", "Damaged")), CancellationToken.None);

        Assert.Equal(OperationsHoldReasons.Damaged, _package.HoldReasons);
        Assert.True(_package.State.IsBlocked);

        OperationsTrackingResult voided = await _tracker.RecordAsync(Draft(OperationsTrackingCodes.EventVoided, "cmd:void-0001", ("why", "entered_in_error")) with { VoidsEventId = hold.Event!.Id, Note = "Wrong package" }, CancellationToken.None);
        OperationsTrackingResult twice = await _tracker.RecordAsync(Draft(OperationsTrackingCodes.EventVoided, "cmd:void-0002", ("why", "entered_in_error")) with { VoidsEventId = hold.Event.Id }, CancellationToken.None);
        OperationsTrackingResult voidAVoid = await _tracker.RecordAsync(Draft(OperationsTrackingCodes.EventVoided, "cmd:void-0003", ("why", "entered_in_error")) with { VoidsEventId = voided.Event!.Id }, CancellationToken.None);

        Assert.True(voided.Created);
        Assert.Equal("ops.tracking.void_target_invalid", twice.Error?.Code);
        Assert.Equal("ops.tracking.void_target_invalid", voidAVoid.Error?.Code);
        Assert.Equal(OperationsHoldReasons.None, _package.HoldReasons);
        Assert.Equal(OperationsTrackingProjection.Replay(_store.Events), _package.State);
    }

    [Fact]
    public async Task CustomerLines_KeepOnlyCustomerSafeArguments_AndPublicShowsOnlyReachedMilestones()
    {
        await _tracker.RecordAsync(Draft(OperationsTrackingCodes.Cancelled, "cmd:cancel-01"), CancellationToken.None);
        await _tracker.RecordAsync(Draft(OperationsTrackingCodes.ReceivedAtOrigin, "scan:late-01", ("warehouse", "MIA")) with { OccurredUtc = Now.AddMinutes(1) }, CancellationToken.None);

        Assert.Equal(OperationsMilestone.Cancelled, _package.Milestone);
        Assert.Equal(OperationsMilestone.Cancelled, Assert.Single(OperationsTrackingViews.ForPublic(_store.Events)).Milestone);
    }

    [Fact]
    public async Task ARacedKey_IsRecoveredAsARepeat()
    {
        OperationsTrackingDraft draft = Draft(OperationsTrackingCodes.ReceivedAtOrigin, "scan:race-01", ("warehouse", "MIA"));
        OperationsTrackingResult first = await _tracker.RecordAsync(draft, CancellationToken.None);

        OperationsTrackingResult? recovered = await _tracker.RecoverRepeatAsync(draft, CancellationToken.None);

        Assert.False(recovered!.Created);
        Assert.Same(first.Event, recovered.Event);
        Assert.Null(await _tracker.RecoverRepeatAsync(draft with { Code = OperationsTrackingCodes.PutAway }, CancellationToken.None));
    }

    [Fact]
    public async Task AnUnknownPackage_IsRefused()
    {
        OperationsTrackingResult result = await _tracker.RecordAsync(Draft(OperationsTrackingCodes.PreAlerted, "cmd:none-0001") with { SubjectId = Guid.NewGuid() }, CancellationToken.None);

        Assert.Equal("ops.common.not_found", result.Error?.Code);
        Assert.Empty(_store.Events);
    }

    [Fact]
    public async Task Views_ShowEachReaderWhatTheyMaySee()
    {
        await _tracker.RecordAsync(Draft(OperationsTrackingCodes.ReceivedAtOrigin, "scan:0003", ("warehouse", "MIA")) with { Note = "Box crushed on one side", LocationCode = "MIA-A-03" }, CancellationToken.None);
        await _tracker.RecordAsync(Draft(OperationsTrackingCodes.PutAway, "scan:0004", ("location", "A-1")), CancellationToken.None);
        OperationsTrackingResult hold = await _tracker.RecordAsync(Draft(OperationsTrackingCodes.HoldOpened, "cmd:hold-0002", ("reason", "Damaged")), CancellationToken.None);
        await _tracker.RecordAsync(Draft(OperationsTrackingCodes.EventVoided, "cmd:void-0004", ("why", "entered_in_error")) with { VoidsEventId = hold.Event!.Id }, CancellationToken.None);

        IReadOnlyList<OperationsTrackingLine> staff = OperationsTrackingViews.ForStaff(_store.Events);
        IReadOnlyList<OperationsTrackingLine> customer = OperationsTrackingViews.ForCustomer(_store.Events);

        Assert.Equal(4, staff.Count);
        Assert.Contains(staff, line => line.Code == OperationsTrackingCodes.HoldOpened && line.Voided);
        OperationsTrackingLine received = Assert.Single(customer);
        Assert.Equal(OperationsTrackingCodes.ReceivedAtOrigin, received.Code);
        Assert.Null(received.Note);
        Assert.Null(received.ActorUserId);
        Assert.Null(received.LocationCode);
        Assert.Equal("MIA", received.Arguments["warehouse"]);
        Assert.Equal(OperationsMilestone.ReceivedAtOrigin, Assert.Single(OperationsTrackingViews.ForPublic(_store.Events)).Milestone);
    }

    private OperationsTrackingDraft Draft(string code, string key, params (string Name, string Value)[] arguments) =>
        new(OperationsTrackingSubject.Parcel, _package.Id, code, arguments.ToDictionary(item => item.Name, item => item.Value), Now, key, OperationsTrackingActor.Staff, OperationsTrackingSource.Scan)
        {
            ActorUserId = Guid.NewGuid()
        };
}
