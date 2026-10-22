using Vixly.Operations.Application.Arrivals;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Application.Delivery;
using Vixly.Operations.Application.Events;
using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Pickup;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Delivery;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Pickup;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Delivery.RecordOperationsDeliveryStop;

// =========================================================
// FILE: RecordOperationsDeliveryStopUseCase.cs
//
// PURPOSE:
// Record how a stop went: delivered with the proof the company asks and the cash collected, or failed with a reason (Slice 7 step 3, R-7, R-12 to R-15).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.delivery.plan at its warehouse (run sheet), or its own driver on the phone page when the company chose it (R-12)
// - The ID's kind and last four characters only, never the full number
// - Recording the same result twice does nothing more
// =========================================================
public sealed class RecordOperationsDeliveryStopUseCase
{
    private readonly OperationsDeliveryDesk _desk;

    private readonly IOperationsPickupRepository _pickups;

    private readonly IOperationsDeliveryRepository _routes;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly OperationsTracker _tracker;

    private readonly OperationsOutboxWriter _outbox;

    private readonly IOperationsFileStore _files;

    private readonly IOperationsImageInspector _images;

    private readonly OperationsPickupCodes _codes;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public RecordOperationsDeliveryStopUseCase(
        OperationsDeliveryDesk desk,
        IOperationsPickupRepository pickups,
        IOperationsDeliveryRepository routes,
        IOperationsReceivingRepository receiving,
        OperationsTracker tracker,
        OperationsOutboxWriter outbox,
        IOperationsFileStore files,
        IOperationsImageInspector images,
        OperationsPickupCodes codes,
        IOperationsSettingsRepository settings,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _pickups = pickups ?? throw new ArgumentNullException(nameof(pickups));
        _routes = routes ?? throw new ArgumentNullException(nameof(routes));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _images = images ?? throw new ArgumentNullException(nameof(images));
        _codes = codes ?? throw new ArgumentNullException(nameof(codes));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<RecordOperationsDeliveryStopResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        RecordOperationsDeliveryStopRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, OperationsDeliveryRouteView? view, bool planner, Error? refused) = await _desk.RouteForAsync(context, request.RouteId, cancellationToken);

        if (refused is not null || view is null)
        {
            return RecordOperationsDeliveryStopResponse.Failure(refused ?? OperationsDeliveryErrors.NotFound);
        }

        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());

        // The driver records on the phone page when the company chose it; staff enter the run sheet otherwise (R-12).
        if (!planner && settings.DeliveryRecording != OperationsDeliveryRecording.Phone)
        {
            return RecordOperationsDeliveryStopResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        if (view.Stops.FirstOrDefault(item => item.Id == request.StopId) is not { } stop || view.Shipments.GetValueOrDefault(stop.ShipmentId) is not { } shipment)
        {
            return RecordOperationsDeliveryStopResponse.Failure(OperationsDeliveryErrors.NotFound);
        }

        // A double click: the same result recorded once.
        if (stop.IsDone)
        {
            return stop.Status == (request.Delivered ? OperationsStopStatus.Delivered : OperationsStopStatus.Failed)
                ? RecordOperationsDeliveryStopResponse.Success()
                : RecordOperationsDeliveryStopResponse.Failure(OperationsDeliveryErrors.StopDone);
        }

        if (view.Route.Status != OperationsRouteStatus.Out)
        {
            return RecordOperationsDeliveryStopResponse.Failure(OperationsDeliveryErrors.NotOut);
        }

        IReadOnlyList<OperationsParcel> parcels = view.ParcelsOf(shipment.Id);
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        Guid userId = access!.User.Id;

        if (!request.Delivered)
        {
            return await FailAsync(view, stop, shipment, parcels, settings, request, userId, utcNow, cancellationToken);
        }

        (decimal due, bool waiting, IReadOnlyList<Guid> owedInvoices) = await _desk.DueAsync(shipment.Id, cancellationToken);

        // Held by customs or turned back since the route left (review): it cannot be handed over.
        if (parcels.Count == 0 || !parcels.All(OperationsDeliveryDesk.Deliverable))
        {
            return RecordOperationsDeliveryStopResponse.Failure(OperationsDeliveryErrors.NotDeliverable);
        }

        if (OperationsDeliveryDesk.Blocked(due, waiting, settings, view.Warehouse))
        {
            return RecordOperationsDeliveryStopResponse.Failure(OperationsPickupErrors.PayFirst);
        }

        // Exactly what is owed is collected (R-13); a customer who cannot pay is a failed stop (not paid).
        decimal cash = request.Cash ?? 0m;

        if (cash != due)
        {
            return RecordOperationsDeliveryStopResponse.Failure(OperationsDeliveryErrors.CashInvalid);
        }

        // Entered from the printed run sheet: the receiver signed the paper; no photo was taken and no code was typed at the door (R-12, review).
        bool fromSheet = planner && settings.DeliveryRecording == OperationsDeliveryRecording.Sheet;
        OperationsDeliveryProofs proofs = fromSheet
            ? settings.DeliveryProofs & ~(OperationsDeliveryProofs.Signature | OperationsDeliveryProofs.Photo | OperationsDeliveryProofs.Code)
            : settings.DeliveryProofs;

        if (proofs.HasFlag(OperationsDeliveryProofs.Id) && (request.IdKind is null || string.IsNullOrWhiteSpace(request.IdLast)))
        {
            return RecordOperationsDeliveryStopResponse.Failure(OperationsPickupErrors.IdRequired);
        }

        byte[]? signature = request.Signature is { Length: > 60 and <= SignatureMaxBytes } drawn && drawn.AsSpan(0, 8).SequenceEqual(PngHeader)
            && _images.Inspect(drawn) is { ContentType: "image/png" } ? drawn : null;

        if ((proofs.HasFlag(OperationsDeliveryProofs.Signature) && signature is null) || (request.Signature is { Length: > 0 } && signature is null))
        {
            return RecordOperationsDeliveryStopResponse.Failure(OperationsPickupErrors.SignatureRequired);
        }

        OperationsImageInfo? photo = request.Photo is { Length: > 0 and <= PhotoMaxBytes } taken ? _images.Inspect(taken) : null;

        if ((proofs.HasFlag(OperationsDeliveryProofs.Photo) && photo is null) || (request.Photo is { Length: > 0 } && photo is null))
        {
            return RecordOperationsDeliveryStopResponse.Failure(OperationsPickupErrors.PhotoRequired);
        }

        bool codeChecked = false;

        if (proofs.HasFlag(OperationsDeliveryProofs.Code))
        {
            if (_codes.Current(shipment) is null)
            {
                return RecordOperationsDeliveryStopResponse.Failure(shipment.PickupCodeLocked ? OperationsPickupErrors.CodeLocked : OperationsPickupErrors.CodeNotSent);
            }

            // Every try is counted before the code is compared, in one statement; five stop the code (as at the counter).
            if (!await _pickups.CountCodeTryAsync(shipment, cancellationToken))
            {
                return RecordOperationsDeliveryStopResponse.Failure(OperationsPickupErrors.CodeLocked);
            }

            codeChecked = _codes.Matches(shipment, request.Code);

            if (!codeChecked)
            {
                _audit.Record(userId, "ops.delivery.code_failed", nameof(OperationsShipment), shipment.Id, after: new { tries = shipment.PickupCodeFailures });
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return RecordOperationsDeliveryStopResponse.Failure(shipment.PickupCodeLocked ? OperationsPickupErrors.CodeLocked : OperationsPickupErrors.CodeWrong);
            }

            shipment.PickupCodeAccepted(utcNow);
        }

        // The proof files are written first; whatever does not end in a saved stop removes them.
        List<string> saved = new();
        bool committed = false;

        try
        {
            string? signatureKey = signature is null ? null : await _files.SaveAsync(context.RequiredTenantId, "deliveries", signature, "image/png", cancellationToken);

            if (signatureKey is not null)
            {
                saved.Add(signatureKey);
            }

            string? photoKey = photo is null ? null : await _files.SaveAsync(context.RequiredTenantId, "deliveries", request.Photo!, photo.ContentType, cancellationToken);

            if (photoKey is not null)
            {
                saved.Add(photoKey);
            }

            try
            {
                stop.Deliver(request.ReceiverName, request.IdKind, request.IdLast, signatureKey, photoKey, codeChecked, fromSheet, cash, userId, utcNow, owedInvoices);
                view.Route.StopDone(cash, view.Stops.All(item => item.IsDone), utcNow);
            }
            catch (DomainException exception)
            {
                _unitOfWork.DiscardChanges();
                return RecordOperationsDeliveryStopResponse.Failure(exception.Error);
            }

            if (await _tracker.RecordManyAsync(parcels.Select(parcel => (new OperationsTrackingDraft(OperationsTrackingSubject.Parcel, parcel.Id, OperationsTrackingCodes.Delivered,
                new Dictionary<string, string>(StringComparer.Ordinal), utcNow, "delivered:" + stop.Id.ToString("N") + ":" + parcel.Id.ToString("N"), OperationsTrackingActor.Staff,
                OperationsTrackingSource.Manual) { ActorUserId = userId }, parcel)).ToList(), cancellationToken) is { } failed)
            {
                _unitOfWork.DiscardChanges();
                return RecordOperationsDeliveryStopResponse.Failure(failed);
            }

            foreach (OperationsParcel parcel in parcels)
            {
                parcel.OffRoute(false, utcNow);
            }

            // Delivered: once every parcel of the shipment is finished. Parcels of it left waiting at the counter after their last try are announced now (review).
            IReadOnlyList<OperationsParcel> all = await _receiving.ListShipmentParcelsForUpdateAsync(shipment.Id, cancellationToken);

            if (all.All(OperationsEventDesk.Finished))
            {
                shipment.Delivered(utcNow);
            }
            else if (all.All(parcel => OperationsEventDesk.Finished(parcel) || parcel.Milestone == OperationsMilestone.ReadyForPickup) && shipment.PickupReadyUtc is null)
            {
                Announce(shipment, view.Warehouse, settings, request.PortalSlug, utcNow);
            }

            _audit.Record(userId, "ops.delivery.delivered", nameof(OperationsDeliveryRoute), view.Route.Id, after: new
            {
                shipment = shipment.Number, parcels = parcels.Count, id = request.IdKind?.ToString(), signed = signature is not null, photo = photo is not null, code = codeChecked, cash,
                sheet = fromSheet
            });

            if (await _unitOfWork.SaveChangesAsync(cancellationToken))
            {
                committed = true;
                return RecordOperationsDeliveryStopResponse.Success();
            }

            _unitOfWork.DiscardChanges();
            return await AgainAsync(stop.Id, OperationsStopStatus.Delivered, cancellationToken);
        }
        finally
        {
            if (!committed)
            {
                foreach (string file in saved)
                {
                    await _files.DeleteAsync(context.RequiredTenantId, file, CancellationToken.None);
                }
            }
        }
    }

    // Failed: one more try for each parcel; after the last, it waits for pickup here or goes back, as the company chose (R-14).
    private async Task<RecordOperationsDeliveryStopResponse> FailAsync(OperationsDeliveryRouteView view, OperationsDeliveryStop stop, OperationsShipment shipment,
        IReadOnlyList<OperationsParcel> parcels, OperationsTenantSettings settings, RecordOperationsDeliveryStopRequest request, Guid userId, DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        try
        {
            stop.Fail(request.Failure, request.Note, userId, utcNow);
            view.Route.StopDone(0m, view.Stops.All(item => item.IsDone), utcNow);
        }
        catch (DomainException exception)
        {
            _unitOfWork.DiscardChanges();
            return RecordOperationsDeliveryStopResponse.Failure(exception.Error);
        }

        string key = stop.Id.ToString("N");

        if (await _tracker.RecordManyAsync(parcels.Select(parcel => (new OperationsTrackingDraft(OperationsTrackingSubject.Parcel, parcel.Id, OperationsTrackingCodes.DeliveryFailed,
            new Dictionary<string, string>(StringComparer.Ordinal) { ["failure"] = stop.FailureReason! }, utcNow, "failed:" + key + ":" + parcel.Id.ToString("N"), OperationsTrackingActor.Staff,
            OperationsTrackingSource.Manual) { ActorUserId = userId, Note = stop.Note }, parcel)).ToList(), cancellationToken) is { } failed)
        {
            _unitOfWork.DiscardChanges();
            return RecordOperationsDeliveryStopResponse.Failure(failed);
        }

        foreach (OperationsParcel parcel in parcels)
        {
            parcel.OffRoute(true, utcNow);
        }

        List<OperationsParcel> last = parcels.Where(parcel => parcel.DeliveryTries >= settings.DeliveryTries).ToList();
        OperationsWarehouse here = view.Warehouse;

        // A moment after the failure, so the history reads in order.
        DateTimeOffset then = utcNow.AddMilliseconds(1);

        if (last.Count > 0 && settings.AfterFailedDelivery == OperationsAfterFailedDelivery.WaitForPickup)
        {
            foreach (OperationsParcel parcel in last)
            {
                parcel.MarkReady(settings.DestinationStorageFreeDays, settings.DestinationStorage ? settings.DestinationStorageFeePerDay : null, utcNow);
            }

            if (await _tracker.RecordManyAsync(last.Select(parcel => (new OperationsTrackingDraft(OperationsTrackingSubject.Parcel, parcel.Id, OperationsTrackingCodes.ReadyForPickup,
                new Dictionary<string, string>(StringComparer.Ordinal) { ["place"] = here.Name }, then, "ready:" + key + ":" + parcel.Id.ToString("N"), OperationsTrackingActor.Staff,
                OperationsTrackingSource.Manual) { ActorUserId = userId }, parcel)).ToList(), cancellationToken) is { } notReady)
            {
                _unitOfWork.DiscardChanges();
                return RecordOperationsDeliveryStopResponse.Failure(notReady);
            }

            // Ready for pickup only once nothing of the shipment still waits for delivery (review).
            bool allWaiting = (await _receiving.ListShipmentParcelsForUpdateAsync(shipment.Id, cancellationToken)).All(parcel =>
                OperationsEventDesk.Finished(parcel) || parcel.Milestone == OperationsMilestone.ReadyForPickup);

            if (allWaiting)
            {
                Announce(shipment, here, settings, request.PortalSlug, utcNow);
            }
        }
        else if (last.Count > 0
            && await _tracker.RecordManyAsync(last.Select(parcel => (new OperationsTrackingDraft(OperationsTrackingSubject.Parcel, parcel.Id, OperationsTrackingCodes.DeliveryReturning,
                new Dictionary<string, string>(StringComparer.Ordinal), then, "returning:" + key + ":" + parcel.Id.ToString("N"), OperationsTrackingActor.Staff,
                OperationsTrackingSource.Manual) { ActorUserId = userId }, parcel)).ToList(), cancellationToken) is { } notReturning)
        {
            _unitOfWork.DiscardChanges();
            return RecordOperationsDeliveryStopResponse.Failure(notReturning);
        }

        _audit.Record(userId, "ops.delivery.failed", nameof(OperationsDeliveryRoute), view.Route.Id, after: new
        {
            shipment = shipment.Number, parcels = parcels.Count, failure = stop.FailureReason, last = last.Count, then = last.Count > 0 ? settings.AfterFailedDelivery.ToString() : null
        });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return RecordOperationsDeliveryStopResponse.Success();
        }

        _unitOfWork.DiscardChanges();
        return await AgainAsync(stop.Id, OperationsStopStatus.Failed, cancellationToken);
    }

    // Ready for pickup at this warehouse: the shipment marked, the customer emailed when the company sends that email.
    private void Announce(OperationsShipment shipment, OperationsWarehouse here, OperationsTenantSettings settings, string? portalSlug, DateTimeOffset utcNow)
    {
        shipment.MarkPickupReady(utcNow);

        if (settings.PickupEmails && !string.IsNullOrWhiteSpace(portalSlug))
        {
            _outbox.Write(Vixly.Operations.Application.Arrivals.OperationsPickupNotice.MessageType, here.TenantId, shipment.Id, new Vixly.Operations.Application.Arrivals.OperationsPickupNotice(
                shipment.CustomerId, portalSlug, shipment.Number ?? string.Empty, here.Name,
                string.Join(", ", new[] { here.Address.Line1, here.Address.Line2, here.Address.City, here.Address.PostalCode }.Where(part => !string.IsNullOrWhiteSpace(part))),
                here.OpeningHours));
        }
    }

    // A double click: the same result already saved by the first answers as done.
    private async Task<RecordOperationsDeliveryStopResponse> AgainAsync(Guid stopId, OperationsStopStatus wanted, CancellationToken cancellationToken) =>
        await _routes.FindStopAsync(stopId, cancellationToken) is { } saved && saved.Status == wanted
            ? RecordOperationsDeliveryStopResponse.Success()
            : RecordOperationsDeliveryStopResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);

    private const int SignatureMaxBytes = 200 * 1024;

    private const int PhotoMaxBytes = 10 * 1024 * 1024;

    private static ReadOnlySpan<byte> PngHeader => new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
}
