using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.Tracking;

// =========================================================
// FILE: OperationsTracker.cs
//
// PURPOSE:
// The one way to write a tracking event (ADR-0021 decisions 1 to 4).
//
// RESPONSIBILITIES:
// - Turn a repeat into the first event (idempotency)
// - Check a void against its target
// - Re-project the package in the same unit of work (D-R6)
//
// IMPORTANT NOTES:
// - The caller saves, so the event, the package state and the caller's own change commit together
// - Carrier feeds, partner scans and the handheld apps will use the same service
// =========================================================
public sealed class OperationsTracker
{
    private readonly IOperationsTrackingRepository _tracking;

    private readonly IOperationsTenantScope _scope;

    private readonly TimeProvider _timeProvider;

    public OperationsTracker(IOperationsTrackingRepository tracking, IOperationsTenantScope scope, TimeProvider timeProvider)
    {
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    // After a save that failed because another writer stored the same key first (two scans racing, an
    // offline queue replayed while the first request was in flight), the caller asks again here with a
    // fresh unit of work and gets the stored event as a repeat (review H2, ADR-0021 decision 4).
    public async Task<OperationsTrackingResult?> RecoverRepeatAsync(OperationsTrackingDraft draft, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(draft);

        return await _tracking.FindStoredByKeyAsync(draft.IdempotencyKey, cancellationToken) is { } stored
            && stored.SubjectKind == draft.SubjectKind && stored.SubjectId == draft.SubjectId && stored.Code == draft.Code
                ? OperationsTrackingResult.Repeat(stored)
                : null;
    }

    // Slice 6B: one event on each of many parcels already loaded (a dispatch's), with three queries in all instead of three per parcel.
    // The first refusal is returned and nothing after it is recorded; the caller abandons its save.
    public async Task<Vixly.Shared.Kernel.Errors.Error?> RecordManyAsync(IReadOnlyList<(OperationsTrackingDraft Draft, OperationsParcel Parcel)> items, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(items);
        Guid tenantId = _scope.TenantId ?? throw new InvalidOperationException("Tracking events are written inside a tenant.");

        if (items.Count == 0)
        {
            return null;
        }

        Dictionary<string, OperationsTrackingEvent> earlier = (await _tracking.FindByKeysAsync(items.Select(item => item.Draft.IdempotencyKey).Where(key => !string.IsNullOrWhiteSpace(key)).ToList(),
            cancellationToken)).GroupBy(item => item.IdempotencyKey!, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        ILookup<Guid, OperationsTrackingEvent> histories = (await _tracking.ListForSubjectsAsync(OperationsTrackingSubject.Parcel, items.Select(item => item.Parcel.Id).ToList(), cancellationToken))
            .ToLookup(item => item.SubjectId);
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        foreach ((OperationsTrackingDraft draft, OperationsParcel parcel) in items)
        {
            if (draft.SubjectKind != OperationsTrackingSubject.Parcel || draft.SubjectId != parcel.Id)
            {
                throw new ArgumentException("Each draft is on its own parcel.", nameof(items));
            }

            if (!string.IsNullOrWhiteSpace(draft.IdempotencyKey) && earlier.TryGetValue(draft.IdempotencyKey, out OperationsTrackingEvent? repeat))
            {
                if (repeat.SubjectId == draft.SubjectId && repeat.Code == draft.Code)
                {
                    continue;
                }

                return OperationsTrackingApplicationErrors.IdempotencyKeyReused;
            }

            List<OperationsTrackingEvent> history = histories[parcel.Id].ToList();

            if (draft.VoidsEventId is { } target
                && (history.FirstOrDefault(item => item.Id == target) is not { VoidsEventId: null } || history.Any(item => item.VoidsEventId == target)))
            {
                return OperationsTrackingApplicationErrors.VoidTargetInvalid;
            }

            OperationsTrackingEvent trackingEvent;

            try
            {
                trackingEvent = OperationsTrackingEvent.Record(tenantId, draft, utcNow);
            }
            catch (DomainException exception)
            {
                return exception.Error;
            }

            _tracking.Add(trackingEvent);
            parcel.Project(OperationsTrackingProjection.Replay(history.Append(trackingEvent)), utcNow);
        }

        return null;
    }

    public async Task<OperationsTrackingResult> RecordAsync(OperationsTrackingDraft draft, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(draft);
        Guid tenantId = _scope.TenantId ?? throw new InvalidOperationException("Tracking events are written inside a tenant.");

        if (!string.IsNullOrWhiteSpace(draft.IdempotencyKey) && await _tracking.FindByKeyAsync(draft.IdempotencyKey, cancellationToken) is { } earlier)
        {
            return earlier.SubjectKind == draft.SubjectKind && earlier.SubjectId == draft.SubjectId && earlier.Code == draft.Code
                ? OperationsTrackingResult.Repeat(earlier)
                : OperationsTrackingResult.Failure(OperationsTrackingApplicationErrors.IdempotencyKeyReused);
        }

        OperationsParcel? parcel = null;

        if (draft.SubjectKind == OperationsTrackingSubject.Parcel
            && (parcel = await _tracking.GetParcelAsync(draft.SubjectId, cancellationToken)) is null)
        {
            return OperationsTrackingResult.Failure(OperationsCommonErrors.NotFound);
        }

        IReadOnlyList<OperationsTrackingEvent> history = await _tracking.ListForSubjectAsync(draft.SubjectKind, draft.SubjectId, cancellationToken);

        if (draft.VoidsEventId is { } target
            && (history.FirstOrDefault(item => item.Id == target) is not { VoidsEventId: null } || history.Any(item => item.VoidsEventId == target)))
        {
            return OperationsTrackingResult.Failure(OperationsTrackingApplicationErrors.VoidTargetInvalid);
        }

        OperationsTrackingEvent trackingEvent;
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        try
        {
            trackingEvent = OperationsTrackingEvent.Record(tenantId, draft, utcNow);
        }
        catch (DomainException exception)
        {
            return OperationsTrackingResult.Failure(exception.Error);
        }

        _tracking.Add(trackingEvent);
        parcel?.Project(OperationsTrackingProjection.Replay(history.Append(trackingEvent)), utcNow);
        return OperationsTrackingResult.New(trackingEvent);
    }
}
