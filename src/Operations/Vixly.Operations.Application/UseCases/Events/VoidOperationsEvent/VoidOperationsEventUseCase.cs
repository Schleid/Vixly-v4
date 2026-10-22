using Vixly.Operations.Application.Events;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Events;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Events.VoidOperationsEvent;

// =========================================================
// FILE: VoidOperationsEventUseCase.cs
//
// PURPOSE:
// Void an event recorded by hand, with a reason (Slice 6C, E-7): its copies on parcels go with it.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.events.add at the subject's warehouses (E-6)
// - The customer stops seeing it
// =========================================================
public sealed class VoidOperationsEventUseCase
{
    private readonly OperationsEventDesk _desk;

    private readonly IOperationsEventRepository _events;

    private readonly IOperationsTrackingRepository _tracking;

    private readonly OperationsTracker _tracker;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public VoidOperationsEventUseCase(
        OperationsEventDesk desk,
        IOperationsEventRepository events,
        IOperationsTrackingRepository tracking,
        OperationsTracker tracker,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<VoidOperationsEventResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        VoidOperationsEventRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, OperationsPermissions.EventsAdd, cancellationToken);

        if (refused is not null)
        {
            return VoidOperationsEventResponse.Failure(refused);
        }

        if (await _tracking.FindEventAsync(request.EventId, cancellationToken) is not { } target
            || await _desk.SubjectAsync(target.SubjectKind, target.SubjectId, cancellationToken) is not { } subject || !subject.Covers(warehouses.Select(item => item.Id)))
        {
            return VoidOperationsEventResponse.Failure(OperationsEventErrors.NotFound);
        }

        // Only what a person recorded here, and the event itself, not one of its copies.
        if (!OperationsTrackingCodes.ManualCodes.Contains(target.Code, StringComparer.Ordinal) || target.CausedByEventId is not null)
        {
            return VoidOperationsEventResponse.Failure(OperationsEventErrors.NotManual);
        }

        string? reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();

        if (reason is null || reason.Length > OperationsTrackingEvent.NoteMaxLength || reason.Any(char.IsControl) || reason.IndexOfAny(['<', '>']) >= 0)
        {
            return VoidOperationsEventResponse.Failure(OperationsEventErrors.VoidReasonRequired);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        Dictionary<string, string> why = new(StringComparer.Ordinal) { ["why"] = "entered_in_error" };
        OperationsTrackingResult voided = await _tracker.RecordAsync(new OperationsTrackingDraft(target.SubjectKind, target.SubjectId, OperationsTrackingCodes.EventVoided, why, utcNow,
            "void:" + target.Id.ToString("N"), OperationsTrackingActor.Staff, OperationsTrackingSource.Manual) { ActorUserId = access!.User.Id, VoidsEventId = target.Id, Note = reason }, cancellationToken);

        // Voided already (a double click): done.
        if (voided.Error is not null)
        {
            return voided.Error == OperationsTrackingApplicationErrors.VoidTargetInvalid
                ? VoidOperationsEventResponse.Success(subject.Kind, subject.Id)
                : VoidOperationsEventResponse.Failure(voided.Error);
        }

        IReadOnlyList<OperationsTrackingEvent> copies = await _events.ListCopiesAsync(target.Id, cancellationToken);
        // The copies' own parcels, wherever they are now, in one query (review).
        Dictionary<Guid, OperationsParcel> parcels = copies.Count == 0 ? new()
            : (await _events.ListParcelsForUpdateAsync(copies.Select(copy => copy.SubjectId).ToList(), cancellationToken)).ToDictionary(parcel => parcel.Id);

        if (await _tracker.RecordManyAsync(copies.Where(copy => parcels.ContainsKey(copy.SubjectId)).Select(copy => (new OperationsTrackingDraft(OperationsTrackingSubject.Parcel, copy.SubjectId,
            OperationsTrackingCodes.EventVoided, why, utcNow, "void:" + copy.Id.ToString("N"), OperationsTrackingActor.Staff, OperationsTrackingSource.Manual)
            { ActorUserId = access.User.Id, VoidsEventId = copy.Id, Note = reason }, parcels[copy.SubjectId])).ToList(), cancellationToken) is { } refusedCopy)
        {
            _unitOfWork.DiscardChanges();
            return VoidOperationsEventResponse.Failure(refusedCopy);
        }

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return VoidOperationsEventResponse.Success(subject.Kind, subject.Id);
        }

        _unitOfWork.DiscardChanges();
        return VoidOperationsEventResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
