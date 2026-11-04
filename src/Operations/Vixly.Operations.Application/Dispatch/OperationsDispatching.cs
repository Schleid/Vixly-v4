using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.Dispatch;

// =========================================================
// FILE: OperationsDispatching.cs
//
// PURPOSE:
// The events of a dispatch and of the parcels in it (Slice 6B, D-5).
//
// RESPONSIBILITIES:
// - Write the dispatch's own events: closed, departed, arrived, cancelled
// - Write each parcel's manifested, departed origin, in transit and arrived in the destination country, at the time it happened
// - Void the manifested events when a closed dispatch is cancelled
//
// IMPORTANT NOTES:
// - The caller saves; a refused event abandons the caller's save
// - Each parcel event has a key from the dispatch, the leg and the parcel, so a repeat writes nothing
// - The parcels are loaded once and recorded in bulk (review: three queries in all, not three per parcel)
// =========================================================
public sealed class OperationsDispatching
{
    private readonly IOperationsDispatchRepository _dispatches;
    private readonly IOperationsTrackingRepository _tracking;
    private readonly OperationsTracker _tracker;
    private readonly TimeProvider _timeProvider;

    public OperationsDispatching(IOperationsDispatchRepository dispatches, IOperationsTrackingRepository tracking, OperationsTracker tracker, TimeProvider timeProvider)
    {
        _dispatches = dispatches ?? throw new ArgumentNullException(nameof(dispatches));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public static string Mode(OperationsCarrierKind kind) => kind switch
    {
        OperationsCarrierKind.Air => "air",
        OperationsCarrierKind.Ocean => "ocean",
        _ => "ground"
    };

    // Every parcel of the dispatch's bundles, tracked, in one query.
    public Task<IReadOnlyList<OperationsParcel>> ParcelsAsync(OperationsDispatch dispatch, CancellationToken cancellationToken) =>
        _dispatches.ListDispatchParcelsAsync((dispatch ?? throw new ArgumentNullException(nameof(dispatch))).Id, true, cancellationToken);

    // On hold, blocked, waiting for a check or held by customs (Slice 7): it may not be manifested or leave (review).
    public static bool Held(OperationsParcel parcel) =>
        (parcel ?? throw new ArgumentNullException(nameof(parcel))).State.IsBlocked || parcel.HoldReasons != OperationsHoldReasons.None
            || (parcel.Flags & (OperationsParcelFlags.AwaitingCheck | OperationsParcelFlags.CustomsHeld)) != 0;

    // Closed: each parcel is manifested on it.
    public async Task ClosedAsync(OperationsDispatch dispatch, IReadOnlyList<OperationsParcel> parcels, Guid userId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parcels);
        await DispatchEventAsync(dispatch, OperationsTrackingCodes.DispatchClosed, new Dictionary<string, string>(StringComparer.Ordinal), "closed", null, userId, null, cancellationToken);
        DateTimeOffset at = dispatch.ClosedUtc ?? _timeProvider.GetUtcNow();

        // Slice 7 (R-11): onward from a hub, the parcels were manifested at the origin already.
        await ManyAsync(parcels.Where(parcel => parcel.Milestone is null || parcel.Milestone < OperationsMilestone.Manifested).Select(parcel => (Draft(parcel, OperationsTrackingCodes.Manifested, new Dictionary<string, string>(StringComparer.Ordinal) { ["dispatch"] = dispatch.Number }, at,
            ManifestedKey(dispatch, parcel), userId), parcel)).ToList(), cancellationToken);
    }

    // A leg left: the first one departs every parcel from the origin; a later one puts them in transit (D-5).
    public async Task DepartedAsync(OperationsDispatch dispatch, OperationsDispatchLeg leg, bool first, IReadOnlyList<OperationsParcel> parcels, Guid userId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(leg);
        ArgumentNullException.ThrowIfNull(parcels);
        await DispatchEventAsync(dispatch, OperationsTrackingCodes.DispatchDeparted, new Dictionary<string, string>(StringComparer.Ordinal) { ["leg"] = leg.From + " - " + leg.To },
            "departed:" + leg.Id.ToString("N"), leg.DepartedUtc, userId, null, cancellationToken);
        // Slice 7 (R-11): a parcel that left its origin already (onward from a hub) is in transit.
        await ManyAsync(parcels.Select(parcel => (first && parcel.IsAtOrigin
            ? Draft(parcel, OperationsTrackingCodes.DepartedOrigin, new Dictionary<string, string>(StringComparer.Ordinal) { ["mode"] = Mode(leg.Kind) }, leg.DepartedUtc!.Value,
                "dsp:" + leg.Id.ToString("N") + ":departed:" + parcel.Id.ToString("N"), userId)
            : Draft(parcel, OperationsTrackingCodes.InTransit, new Dictionary<string, string>(StringComparer.Ordinal) { ["place"] = leg.From }, leg.DepartedUtc!.Value,
                "dsp:" + leg.Id.ToString("N") + ":transit:" + parcel.Id.ToString("N"), userId), parcel)).ToList(), cancellationToken);
    }

    // A leg arrived; the last one brings every parcel to the destination country (D-5).
    public async Task ArrivedAsync(OperationsDispatch dispatch, OperationsDispatchLeg leg, bool last, IReadOnlyList<OperationsParcel> parcels, Guid userId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dispatch);
        ArgumentNullException.ThrowIfNull(leg);
        ArgumentNullException.ThrowIfNull(parcels);
        await DispatchEventAsync(dispatch, OperationsTrackingCodes.DispatchArrived, new Dictionary<string, string>(StringComparer.Ordinal) { ["leg"] = leg.From + " - " + leg.To },
            "arrived:" + leg.Id.ToString("N"), leg.ArrivedUtc, userId, null, cancellationToken);

        // Slice 7 (R-11): onward from a hub, already in the destination country; the final warehouse's scan says it arrived.
        if (last)
        {
            await ManyAsync(parcels.Where(parcel => parcel.Milestone is null || parcel.Milestone < OperationsMilestone.ArrivedDestinationCountry).Select(parcel => (Draft(parcel, OperationsTrackingCodes.ArrivedDestinationCountry,
                new Dictionary<string, string>(StringComparer.Ordinal) { ["country"] = dispatch.DestinationCountryCode }, leg.ArrivedUtc!.Value,
                "dsp:" + dispatch.Id.ToString("N") + ":arrived:" + parcel.Id.ToString("N"), userId), parcel)).ToList(), cancellationToken);
        }
    }

    // Cancelled once closed: the parcels' manifested events are voided, so their milestone goes back.
    public async Task CancelledAsync(OperationsDispatch dispatch, IReadOnlyList<OperationsParcel> parcels, bool wasClosed, Guid userId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parcels);
        await DispatchEventAsync(dispatch, OperationsTrackingCodes.DispatchCancelled, new Dictionary<string, string>(StringComparer.Ordinal), "cancelled", null, userId, dispatch.CancelReason, cancellationToken);

        if (!wasClosed)
        {
            return;
        }

        // A manifested event voided already (by hand) is left as it is (review).
        HashSet<Guid> voided = (await _tracking.ListForSubjectsAsync(OperationsTrackingSubject.Parcel, parcels.Select(parcel => parcel.Id).ToList(), cancellationToken))
            .Where(item => item.VoidsEventId is not null).Select(item => item.VoidsEventId!.Value).ToHashSet();
        Dictionary<Guid, OperationsTrackingEvent> manifested = (await _tracking.FindByKeysAsync(parcels.Select(parcel => ManifestedKey(dispatch, parcel)).ToList(), cancellationToken))
            .Where(item => !voided.Contains(item.Id)).GroupBy(item => item.SubjectId).ToDictionary(group => group.Key, group => group.First());
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        await ManyAsync(parcels.Where(parcel => manifested.ContainsKey(parcel.Id)).Select(parcel => (new OperationsTrackingDraft(OperationsTrackingSubject.Parcel, parcel.Id,
            OperationsTrackingCodes.EventVoided, new Dictionary<string, string>(StringComparer.Ordinal) { ["why"] = "dispatch_cancelled" }, utcNow, "void:" + manifested[parcel.Id].Id.ToString("N"),
            OperationsTrackingActor.Staff, OperationsTrackingSource.Manual) { ActorUserId = userId, VoidsEventId = manifested[parcel.Id].Id }, parcel)).ToList(), cancellationToken);
    }

    private static string ManifestedKey(OperationsDispatch dispatch, OperationsParcel parcel) => "dsp:" + dispatch.Id.ToString("N") + ":manifested:" + parcel.Id.ToString("N");

    private async Task DispatchEventAsync(OperationsDispatch dispatch, string code, IReadOnlyDictionary<string, string> arguments, string step, DateTimeOffset? at, Guid userId, string? note,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dispatch);
        OperationsTrackingDraft draft = new(OperationsTrackingSubject.Dispatch, dispatch.Id, code, arguments, at ?? _timeProvider.GetUtcNow(),
            "dispatch:" + dispatch.Id.ToString("N") + ":" + step, OperationsTrackingActor.Staff, OperationsTrackingSource.Manual) { ActorUserId = userId, Note = note };

        if ((await _tracker.RecordAsync(draft, cancellationToken)).Error is { } refused)
        {
            throw new InvalidOperationException("The " + draft.Code + " event of " + draft.SubjectId + " was refused: " + refused.Code);
        }
    }

    private static OperationsTrackingDraft Draft(OperationsParcel parcel, string code, IReadOnlyDictionary<string, string> arguments, DateTimeOffset at, string key, Guid userId) =>
        new(OperationsTrackingSubject.Parcel, parcel.Id, code, arguments, at, key, OperationsTrackingActor.Staff, OperationsTrackingSource.Manual) { ActorUserId = userId };

    private async Task ManyAsync(IReadOnlyList<(OperationsTrackingDraft Draft, OperationsParcel Parcel)> items, CancellationToken cancellationToken)
    {
        if (await _tracker.RecordManyAsync(items, cancellationToken) is { } refused)
        {
            throw new InvalidOperationException("A dispatch event was refused: " + refused.Code);
        }
    }
}
