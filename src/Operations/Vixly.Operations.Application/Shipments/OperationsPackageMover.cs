using Vixly.Operations.Application.Inspection;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.Shipments;

// =========================================================
// FILE: OperationsPackageMover.cs
//
// PURPOSE:
// Puts a customer's package into one of its shipments, moves it to another, or takes it out (Slice 4D; owner, 2026-10-19).
//
// RESPONSIBILITIES:
// - A pre-alert not yet received: only the pre-alert moves
// - A received parcel still at its origin: the parcel moves with its pre-alert; taken out, it waits in the waiting shipment
// - The "awaiting shipment" hold opens when the parcel is in none of the customer's shipments and ends when it is in one
//
// IMPORTANT NOTES:
// - Only into the customer's own open shipment with instructions, from the parcel's warehouse
// - Each move changes the shipments' rows, so a cancel at the same moment clashes
// =========================================================
public sealed class OperationsPackageMover
{
    private readonly IOperationsReceivingRepository _receiving;

    private readonly OperationsShipmentPlacement _placement;

    private readonly OperationsTracker _tracker;

    private readonly OperationsInspectionSelection _selection;

    private readonly IOperationsTrackingRepository _tracking;

    public OperationsPackageMover(IOperationsReceivingRepository receiving, OperationsShipmentPlacement placement, OperationsTracker tracker, OperationsInspectionSelection selection,
        IOperationsTrackingRepository tracking)
    {
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _placement = placement ?? throw new ArgumentNullException(nameof(placement));
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        _selection = selection ?? throw new ArgumentNullException(nameof(selection));
    }

    // The customer's own package, by its pre-alert or its parcel; another customer's answers "not found" (S-3).
    public async Task<Error?> PlaceForCustomerAsync(Guid customerId, Guid? preAlertId, Guid? parcelId, OperationsShipment? target, DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        // One package at a time: a parcel always moves with its own pre-alert (review of 4D).
        if (preAlertId is not null && parcelId is not null)
        {
            return OperationsReceivingErrors.ParcelNotFound;
        }

        OperationsPreAlert? preAlert = preAlertId is { } announced ? await _receiving.GetPreAlertAsync(announced, cancellationToken) : null;

        if (preAlertId is not null && (preAlert is null || preAlert.CustomerId != customerId))
        {
            return OperationsReceivingErrors.PreAlertNotFound;
        }

        Guid? received = parcelId ?? (preAlert?.Status == OperationsPreAlertStatus.Received ? preAlert.ParcelId : null);

        if (received is not { } id)
        {
            return preAlert is null ? OperationsReceivingErrors.ParcelNotFound : MovePreAlert(preAlert, target, utcNow);
        }

        if (await _tracking.GetParcelAsync(id, cancellationToken) is not { } parcel || parcel.CustomerId != customerId || (preAlert is not null && parcel.PreAlertId != preAlert.Id))
        {
            return OperationsReceivingErrors.ParcelNotFound;
        }

        string key = "mov:" + parcel.Id.ToString("N") + ":" + (target?.Id.ToString("N") ?? "none") + ":" + utcNow.UtcTicks;
        return await MoveParcelAsync(parcel, target, key, OperationsTrackingActor.Customer, OperationsTrackingSource.Customer, null, utcNow, cancellationToken);
    }

    // A pre-alert whose package has not arrived.
    public static Error? MovePreAlert(OperationsPreAlert preAlert, OperationsShipment? target, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(preAlert);

        if (preAlert.Status != OperationsPreAlertStatus.Open)
        {
            return OperationsReceivingErrors.PreAlertNotOpen;
        }

        try
        {
            if (target is null)
            {
                preAlert.TakeOut(utcNow);
            }
            else
            {
                preAlert.PlaceIn(target, utcNow);
                target.TakePackage(utcNow);
            }

            return null;
        }
        catch (DomainException exception)
        {
            return exception.Error;
        }
    }

    // A received parcel, with its pre-alert when it has one.
    public async Task<Error?> MoveParcelAsync(OperationsParcel parcel, OperationsShipment? target, string key, OperationsTrackingActor actor, OperationsTrackingSource source,
        Guid? actorUserId, DateTimeOffset utcNow, CancellationToken cancellationToken, OperationsPreAlert? matched = null)
    {
        ArgumentNullException.ThrowIfNull(parcel);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (!parcel.IsAtOrigin || parcel.WarehouseId is not { } warehouseId || parcel.CustomerId is not { } customerId)
        {
            return OperationsReceivingErrors.ShipmentNotOpen;
        }

        if (target is not null && (target.CustomerId != customerId || !target.HasInstructions))
        {
            return OperationsShipmentErrors.NotFound;
        }

        if (target is not null && target.OriginWarehouseId != warehouseId)
        {
            return OperationsShipmentErrors.ParcelElsewhere;
        }

        OperationsPreAlert? preAlert = matched ?? (parcel.PreAlertId is { } preAlertId ? await _receiving.GetPreAlertAsync(preAlertId, cancellationToken) : null);
        OperationsShipment shipment = target ?? await _placement.WaitingAsync(parcel.TenantId, customerId, warehouseId, utcNow, cancellationToken);

        try
        {
            parcel.JoinShipment(shipment, utcNow);
            shipment.TakePackage(utcNow);

            if (preAlert is not null && target is null)
            {
                preAlert.TakeOut(utcNow);
            }
            else if (preAlert is not null)
            {
                preAlert.PlaceIn(shipment, utcNow);
            }
        }
        catch (DomainException exception)
        {
            return exception.Error;
        }

        bool waiting = parcel.HoldReasons.HasFlag(OperationsHoldReasons.AwaitingShipment);
        string? code = target is null && !waiting ? OperationsTrackingCodes.HoldOpened : target is not null && waiting ? OperationsTrackingCodes.HoldReleased : null;

        if (code is not null)
        {
            OperationsTrackingDraft step = new(OperationsTrackingSubject.Parcel, parcel.Id, code,
                new Dictionary<string, string>(StringComparer.Ordinal) { ["reason"] = nameof(OperationsHoldReasons.AwaitingShipment) }, utcNow, key, actor, source)
            {
                ActorUserId = actorUserId
            };

            if ((await _tracker.RecordAsync(step, cancellationToken)).Error is { } refused)
            {
                return refused;
            }
        }

        // Its destination may bring the company's inspection rules into play (Slice 3C).
        if (target is not null)
        {
            await _selection.RecheckAsync(parcel, target, utcNow, cancellationToken);
        }

        return null;
    }
}
