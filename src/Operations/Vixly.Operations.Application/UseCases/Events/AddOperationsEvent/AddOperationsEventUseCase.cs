using Vixly.Operations.Application.Events;
using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Events;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Events.AddOperationsEvent;

// =========================================================
// FILE: AddOperationsEventUseCase.cs
//
// PURPOSE:
// Record a note, a delay or an exception by hand (Slice 6C, E-1 to E-5).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.events.add at the warehouse the subject leaves from or goes to (E-6)
// - Milestones are never written here (E-1)
// =========================================================
public sealed class AddOperationsEventUseCase
{
    private readonly OperationsEventDesk _desk;

    private readonly IOperationsEventRepository _events;

    private readonly OperationsTracker _tracker;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsOutboxWriter _outbox;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public AddOperationsEventUseCase(
        OperationsEventDesk desk,
        IOperationsEventRepository events,
        OperationsTracker tracker,
        IOperationsReceivingRepository receiving,
        IOperationsSettingsRepository settings,
        OperationsOutboxWriter outbox,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<AddOperationsEventResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        AddOperationsEventRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, OperationsPermissions.EventsAdd, cancellationToken);

        if (refused is not null)
        {
            return AddOperationsEventResponse.Failure(refused);
        }

        if (await _desk.SubjectAsync(request.Kind, request.Id, cancellationToken) is not { } subject || !subject.Covers(warehouses.Select(item => item.Id)))
        {
            return AddOperationsEventResponse.Failure(OperationsEventErrors.NotFound);
        }

        if (!Enum.IsDefined(request.EventKind))
        {
            return AddOperationsEventResponse.Failure(OperationsEventErrors.KindInvalid);
        }

        Vixly.Operations.Domain.Settings.OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken)
            ?? Vixly.Operations.Domain.Settings.OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        DateTimeOffset at = request.At is { } local ? OperationsTimeZones.At(local, settings.TimeZone) : utcNow;

        if (at > utcNow.AddMinutes(5) || at < utcNow.AddDays(-30))
        {
            return AddOperationsEventResponse.Failure(OperationsEventErrors.TimeInvalid);
        }

        string? note;
        string? message;

        try
        {
            note = OperationsEventText(request.Note, OperationsTrackingEvent.NoteMaxLength);
            message = OperationsEventText(request.Message, 140);
        }
        catch (DomainException exception)
        {
            return AddOperationsEventResponse.Failure(exception.Error);
        }

        Dictionary<string, string> arguments = new(StringComparer.Ordinal);
        OperationsEventCause? cause = null;

        if (request.EventKind == OperationsEventKind.Note)
        {
            if (note is null)
            {
                return AddOperationsEventResponse.Failure(OperationsEventErrors.NoteRequired);
            }
        }
        else
        {
            cause = (await _desk.CausesAsync(request.EventKind, settings, cancellationToken)).FirstOrDefault(item => item.Code == request.Cause);

            if (cause is null)
            {
                return AddOperationsEventResponse.Failure(OperationsEventErrors.ReasonRequired);
            }

            arguments["cause"] = cause.Code;
            Put(arguments, "label_en", cause.TextEn);
            Put(arguments, "label_fr", cause.TextFr);
            Put(arguments, "label_es", cause.TextEs);
            Put(arguments, "message", message);
        }

        string code = request.EventKind switch
        {
            OperationsEventKind.Delay => OperationsTrackingCodes.Delayed,
            OperationsEventKind.Exception => OperationsTrackingCodes.ExceptionRaised,
            _ => OperationsTrackingCodes.Note
        };
        string key = "evt:" + (request.RequestKey ?? Guid.NewGuid()).ToString("N");

        // A double click: the event the first click wrote (ADR-0019).
        OperationsTrackingDraft first = new(subject.Kind, subject.Id, code, arguments, at, key, OperationsTrackingActor.Staff, OperationsTrackingSource.Manual) { ActorUserId = access!.User.Id };

        if (await _tracker.RecoverRepeatAsync(first, cancellationToken) is not null)
        {
            return AddOperationsEventResponse.Success(0);
        }

        OperationsTrackingResult recorded = await _tracker.RecordAsync(new OperationsTrackingDraft(subject.Kind, subject.Id, code, arguments, at, key, OperationsTrackingActor.Staff,
            OperationsTrackingSource.Manual) { ActorUserId = access.User.Id, Note = note }, cancellationToken);

        if (recorded.Error is { } refusedEvent)
        {
            return AddOperationsEventResponse.Failure(refusedEvent);
        }

        int reached = 0;

        // A delay or an exception on a shipment, bundle or dispatch reaches every parcel in it, linked to this event (E-4).
        if (cause is not null)
        {
            // Parcels already delivered, cancelled, returned or disposed of are left alone (review).
            IReadOnlyList<OperationsParcel> parcels = subject.Kind == OperationsTrackingSubject.Parcel ? Array.Empty<OperationsParcel>()
                : (await _desk.ParcelsAsync(subject, cancellationToken)).Where(parcel => !OperationsEventDesk.Finished(parcel)).ToList();
            Dictionary<string, string> copy = new(arguments, StringComparer.Ordinal);

            // A shipment from before numbering has no number to name (review).
            if (subject.Number.Length > 0)
            {
                copy["source"] = subject.Number;
            }

            if (await _tracker.RecordManyAsync(parcels.Select(parcel => (new OperationsTrackingDraft(OperationsTrackingSubject.Parcel, parcel.Id, code, copy, at,
                key + ":" + parcel.Id.ToString("N"), OperationsTrackingActor.Staff, OperationsTrackingSource.Manual)
                { ActorUserId = access.User.Id, CausedByEventId = recorded.Event!.Id }, parcel)).ToList(), cancellationToken) is { } refusedCopy)
            {
                _unitOfWork.DiscardChanges();
                return AddOperationsEventResponse.Failure(refusedCopy);
            }

            reached = parcels.Count;

            // One email per shipment the event reached, or per package not yet in a shipment (E-5), when the company sends them.
            if (settings.EventEmails && !string.IsNullOrWhiteSpace(request.PortalSlug))
            {
                IReadOnlyList<OperationsParcel> reachedParcels = subject.Kind == OperationsTrackingSubject.Parcel
                    ? (await _desk.ParcelsAsync(subject, cancellationToken)).Where(parcel => !OperationsEventDesk.Finished(parcel)).ToList()
                    : parcels;

                foreach (Guid shipmentId in reachedParcels.Where(parcel => parcel.ShipmentId is not null).Select(parcel => parcel.ShipmentId!.Value).Distinct())
                {
                    if (await _receiving.GetShipmentAsync(shipmentId, cancellationToken) is { } shipment)
                    {
                        _outbox.Write(OperationsEventNotice.MessageType, context.RequiredTenantId, shipment.Id, new OperationsEventNotice(shipment.CustomerId, request.PortalSlug,
                            shipment.Number ?? string.Empty, request.EventKind == OperationsEventKind.Exception, cause.Code, cause.TextEn, cause.TextFr, cause.TextEs, message));
                    }
                }

                foreach (OperationsParcel loose in reachedParcels.Where(parcel => parcel.ShipmentId is null && parcel.CustomerId is not null))
                {
                    _outbox.Write(OperationsEventNotice.MessageType, context.RequiredTenantId, loose.Id, new OperationsEventNotice(loose.CustomerId!.Value, request.PortalSlug,
                        loose.Number ?? loose.CarrierTrackingNumber ?? string.Empty, request.EventKind == OperationsEventKind.Exception, cause.Code, cause.TextEn, cause.TextFr, cause.TextEs, message)
                        { Package = true });
                }
            }
        }

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return AddOperationsEventResponse.Success(reached);
        }

        _unitOfWork.DiscardChanges();

        // Two clicks at once: the other one wrote it (review).
        return await _tracker.RecoverRepeatAsync(first, cancellationToken) is not null
            ? AddOperationsEventResponse.Success(0)
            : AddOperationsEventResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }

    private static void Put(Dictionary<string, string> arguments, string name, string? value)
    {
        if (value is not null)
        {
            arguments[name] = value;
        }
    }

    private static string? OperationsEventText(string? value, int max)
    {
        string? text = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        return text is null || (text.Length <= max && !text.Any(char.IsControl) && text.IndexOfAny(['<', '>']) < 0)
            ? text
            : throw new DomainException(OperationsEventErrors.TextInvalid);
    }
}
