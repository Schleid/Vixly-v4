using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Printing;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Printing;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.ScanOperationsParcel;

// =========================================================
// FILE: ScanOperationsParcelUseCase.cs
//
// PURPOSE:
// Receives one package at the dock by its carrier barcode and prints its ID label (Slice 3B, owner 2026-10-18).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Scan only: no typing, weighing or photos at the dock; those come at the inspection station (owner, 2026-10-18)
// - The parcel, its events, its label job and the customer's email (outbox) commit together
// =========================================================
public sealed class ScanOperationsParcelUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsTrackingRepository _tracking;

    private readonly IOperationsCustomerRepository _customers;

    private readonly OperationsParcelMatcher _matcher;

    private readonly OperationsNumbering _numbering;

    private readonly OperationsTracker _tracker;

    private readonly OperationsLabelPrinting _labels;

    private readonly OperationsShipmentPlacement _placement;

    private readonly OperationsOutboxWriter _outbox;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    private readonly IOperationsSettingsRepository _settings;

    public ScanOperationsParcelUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsReceivingRepository receiving,
        IOperationsTrackingRepository tracking,
        IOperationsCustomerRepository customers,
        OperationsParcelMatcher matcher,
        OperationsNumbering numbering,
        OperationsTracker tracker,
        OperationsLabelPrinting labels,
        OperationsShipmentPlacement placement,
        OperationsOutboxWriter outbox,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IOperationsSettingsRepository settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _matcher = matcher ?? throw new ArgumentNullException(nameof(matcher));
        _numbering = numbering ?? throw new ArgumentNullException(nameof(numbering));
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        _labels = labels ?? throw new ArgumentNullException(nameof(labels));
        _placement = placement ?? throw new ArgumentNullException(nameof(placement));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ScanOperationsParcelResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ScanOperationsParcelRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return ScanOperationsParcelResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _receiving.GetDeliveryAsync(request.DeliveryId, cancellationToken) is not { } found
            || await _scope.CoveredAsync(access, OperationsPermissions.ReceivingReceive, found.WarehouseId, cancellationToken) is not { } warehouse)
        {
            return ScanOperationsParcelResponse.Failure(OperationsReceivingErrors.DeliveryNotFound);
        }

        if (!OperationsCommandKey.IsValid(request.ScanKey))
        {
            return ScanOperationsParcelResponse.Failure(OperationsTrackingErrors.IdempotencyKeyInvalid);
        }

        string key = "scn:" + request.ScanKey;

        // The same scan sent twice (a retry, a queued scan sent again after an outage) gives the first answer, without a second label.
        if (await RepeatAsync(key, cancellationToken) is { } repeat)
        {
            return repeat;
        }

        IReadOnlyList<string> candidates = OperationsCarrierBarcode.Candidates(request.Barcode);

        if (candidates.Count == 0)
        {
            return ScanOperationsParcelResponse.Failure(OperationsReceivingErrors.BarcodeUnreadable);
        }

        OperationsLabelPrinter? printer = await _labels.UsableAsync(request.PrinterId, warehouse.Id, OperationsLabelMedia.IdLabel, cancellationToken);

        // A parcel ID label scanned by mistake (or caught by the camera) is never a new parcel: at its own warehouse it prints again.
        if (await _receiving.FindParcelByNumberAsync(candidates[^1], cancellationToken) is { Number: { } own } labelled)
        {
            return labelled.WarehouseId == warehouse.Id && labelled.IsAtOrigin
                ? await ReprintAsync(context.RequiredTenantId, labelled, own, printer, cancellationToken)
                : ScanOperationsParcelResponse.Failure(OperationsReceivingErrors.BarcodeUnreadable);
        }

        OperationsInboundDelivery delivery = found;

        // A second clerk's save can win a race: the same package scanned twice, the customer's first shipment of the day, or a scan on the
        // same delivery (each scan changes the delivery row). Look again and try once more (review M2).
        for (int attempt = 0; attempt < 3; attempt++)
        {
            if (attempt > 0)
            {
                _unitOfWork.DiscardChanges();

                if (await RepeatAsync(key, cancellationToken) is { } raced)
                {
                    return raced;
                }

                if (await _receiving.GetDeliveryAsync(request.DeliveryId, cancellationToken) is not { } reloaded)
                {
                    return ScanOperationsParcelResponse.Failure(OperationsReceivingErrors.DeliveryNotFound);
                }

                delivery = reloaded;
            }

            // The package is already here (scanned again, its label lost or jammed): print its label again, never a second parcel.
            if (await _receiving.FindActiveParcelAsync([warehouse.Id], candidates, cancellationToken) is { Number: { } number } existing)
            {
                return await ReprintAsync(context.RequiredTenantId, existing, number, printer, cancellationToken);
            }

            if (delivery.Status != OperationsDeliveryStatus.Open)
            {
                return ScanOperationsParcelResponse.Failure(OperationsReceivingErrors.DeliveryClosed);
            }

            if (await AttemptAsync(context.RequiredTenantId, access, warehouse, delivery, request, key, candidates, printer, cancellationToken) is { } done)
            {
                return done;
            }
        }

        return ScanOperationsParcelResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }

    private async Task<ScanOperationsParcelResponse?> AttemptAsync(Guid tenantId, OperationsAccessView access, OperationsWarehouse warehouse, OperationsInboundDelivery delivery,
        ScanOperationsParcelRequest request, string key, IReadOnlyList<string> candidates, OperationsLabelPrinter? printer, CancellationToken cancellationToken)
    {
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsParcelMatch match = await _matcher.MatchAsync(candidates, cancellationToken);

        // The customer's own spelling of the number when they announced it; otherwise the most likely reading of the barcode.
        string tracking = match.PreAlert?.TrackingNumber ?? candidates[0];
        string number = await _numbering.NextAsync(OperationsNumberKind.Parcel, cancellationToken);
        OperationsParcel parcel = OperationsParcel.Receive(tenantId, number, delivery, delivery.Carrier ?? OperationsCarrierBarcode.CarrierOf(tracking), tracking, request.Damaged, utcNow);

        // Slice 4D step 3: the storage terms in force when it arrived (free days, fee per day).
        OperationsTenantSettings? terms = await _settings.FindAsync(cancellationToken);
        parcel.TakeStorageTerms(terms?.StorageFreeDays ?? Vixly.Operations.Domain.Holds.OperationsHoldLimits.DefaultUnclaimedDays, terms?.StorageFeePerDay);
        _tracking.Add(parcel);

        bool placed = false;

        if (match.Customer is { } customer)
        {
            // Slice 4D: the shipment the customer put the pre-alert in, or the customer's waiting shipment here; never placed by guesswork (owner, 2026-10-19).
            (OperationsShipment shipment, placed) = await _placement.PlaceAsync(tenantId, customer.Id, warehouse.Id, match.PreAlert, utcNow, cancellationToken);
            parcel.JoinShipment(shipment, utcNow);

            // The pre-alert is the customer's declaration either way; not in a shipment from here, it waits for the customer to place it.
            if (match.PreAlert is { } preAlert)
            {
                parcel.MatchPreAlert(preAlert, utcNow);

                if (!placed)
                {
                    preAlert.TakeOut(utcNow);
                }
            }
        }

        foreach (OperationsTrackingDraft step in Steps(key, parcel, match, placed, request.Damaged, access, warehouse, utcNow))
        {
            if ((await _tracker.RecordAsync(step, cancellationToken)).Error is { } refused)
            {
                // The same scan sent twice at once: the other one was saved first with its own new parcel. Look again; it is a repeat.
                return refused.Equals(Vixly.Operations.Application.Tracking.OperationsTrackingApplicationErrors.IdempotencyKeyReused) ? null : ScanOperationsParcelResponse.Failure(refused);
            }
        }

        if (match.Customer is { } owner)
        {
            OperationsCustomerMailKind kind = match.PreAlert is null || !placed || request.Damaged ? OperationsCustomerMailKind.ParcelActionNeeded : OperationsCustomerMailKind.ParcelReceived;
            _outbox.Write(OperationsReceivingMessages.ParcelNotice, tenantId, parcel.Id, new OperationsParcelNotice(owner.Id, parcel.Id, kind, request.PortalSlug));
        }

        if (printer is not null)
        {
            _labels.QueueParcelLabel(tenantId, printer, number, match.Customer?.Code);
        }

        _audit.Record(access.User.Id, "ops.receiving.received", nameof(OperationsParcel), parcel.Id,
            after: new { number, tracking, outcome = match.Outcome, warehouse = warehouse.Code, damaged = request.Damaged });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ScanOperationsParcelResponse.Success(parcel.Id, number, match.Outcome, match.Customer?.Code, match.Customer?.FullName, printer is not null)
            : null;
    }

    private static IEnumerable<OperationsTrackingDraft> Steps(string key, OperationsParcel parcel, OperationsParcelMatch match, bool placed, bool damaged, OperationsAccessView access,
        OperationsWarehouse warehouse, DateTimeOffset utcNow)
    {
        if (match.PreAlert is { } preAlert)
        {
            yield return new OperationsTrackingDraft(OperationsTrackingSubject.Parcel, parcel.Id, OperationsTrackingCodes.PreAlerted, Arguments(), preAlert.CreatedUtc, key + ":prealert",
                OperationsTrackingActor.Customer, OperationsTrackingSource.Customer);
        }

        yield return Step(key + ":received", parcel, OperationsTrackingCodes.ReceivedAtOrigin, access, warehouse, utcNow, ("warehouse", warehouse.Code));

        if (match.Customer is null)
        {
            yield return Step(key + ":hold:unidentified", parcel, OperationsTrackingCodes.HoldOpened, access, warehouse, utcNow, ("reason", nameof(OperationsHoldReasons.Unidentified)));
        }
        else if (match.PreAlert is null)
        {
            yield return Step(key + ":hold:information", parcel, OperationsTrackingCodes.HoldOpened, access, warehouse, utcNow, ("reason", nameof(OperationsHoldReasons.AwaitingCustomerInformation)));
        }
        else if (!placed)
        {
            yield return Step(key + ":hold:shipment", parcel, OperationsTrackingCodes.HoldOpened, access, warehouse, utcNow, ("reason", nameof(OperationsHoldReasons.AwaitingShipment)));
        }

        // Damage seen at the dock: on hold at once, and on the receipt the driver signs.
        if (damaged)
        {
            yield return Step(key + ":hold:damaged", parcel, OperationsTrackingCodes.HoldOpened, access, warehouse, utcNow, ("reason", nameof(OperationsHoldReasons.Damaged)));
        }
    }

    private async Task<ScanOperationsParcelResponse> ReprintAsync(Guid tenantId, OperationsParcel parcel, string number, OperationsLabelPrinter? printer, CancellationToken cancellationToken)
    {
        OperationsCustomer? customer = parcel.CustomerId is { } customerId ? await _customers.GetAsync(customerId, cancellationToken) : null;

        if (printer is not null)
        {
            _labels.QueueParcelLabel(tenantId, printer, number, customer?.Code);

            if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
            {
                return ScanOperationsParcelResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
            }
        }

        return ScanOperationsParcelResponse.Success(parcel.Id, number, "reprint", customer?.Code, customer?.FullName, printer is not null);
    }

    private async Task<ScanOperationsParcelResponse?> RepeatAsync(string key, CancellationToken cancellationToken)
    {
        if (await _tracking.FindStoredByKeyAsync(key + ":received", cancellationToken) is not { } earlier
            || await _tracking.GetParcelAsync(earlier.SubjectId, cancellationToken) is not { Number: { } number } parcel)
        {
            return null;
        }

        OperationsCustomer? customer = parcel.CustomerId is { } customerId ? await _customers.GetAsync(customerId, cancellationToken) : null;
        return ScanOperationsParcelResponse.Success(parcel.Id, number, "repeat", customer?.Code, customer?.FullName, false);
    }

    private static Dictionary<string, string> Arguments(params (string Name, string Value)[] arguments) =>
        arguments.ToDictionary(item => item.Name, item => item.Value, StringComparer.Ordinal);

    private static OperationsTrackingDraft Step(string key, OperationsParcel parcel, string code, OperationsAccessView access, OperationsWarehouse warehouse, DateTimeOffset utcNow,
        params (string Name, string Value)[] arguments) =>
        new(OperationsTrackingSubject.Parcel, parcel.Id, code, Arguments(arguments), utcNow, key, OperationsTrackingActor.Staff, OperationsTrackingSource.Scan)
        {
            ActorUserId = access.User.Id,
            LocationCode = warehouse.Code
        };
    
}
