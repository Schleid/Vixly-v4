using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.Shipments;

// =========================================================
// FILE: OperationsPreAlertIntake.cs
//
// PURPOSE:
// Announces one package: on its own for a warehouse, or inside one of the customer's shipments (Slice 3B; Slice 4D, owner, 2026-10-19).
//
// RESPONSIBILITIES:
// - Check the package, refuse a second open announcement of the same number
// - A package that arrived first takes the announcement at once and goes where the pre-alert is (its shipment, or the waiting shipment)
//
// IMPORTANT NOTES:
// - The caller saves; nothing is saved here
// - An unidentified package is never given to a customer this way; a clerk assigns it after a check (D-R8)
// - One open pre-alert per customer and number; another customer's announcement neither blocks nor shows (review H1)
// =========================================================
public sealed class OperationsPreAlertIntake
{
    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsTracker _tracker;

    private readonly Vixly.Operations.Application.Inspection.OperationsInspectionSelection _selection;

    private readonly OperationsPackageMover _mover;

    public OperationsPreAlertIntake(IOperationsReceivingRepository receiving, IOperationsSettingsRepository settings, OperationsTracker tracker,
        Vixly.Operations.Application.Inspection.OperationsInspectionSelection selection, OperationsPackageMover mover)
    {
        _selection = selection ?? throw new ArgumentNullException(nameof(selection));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        _mover = mover ?? throw new ArgumentNullException(nameof(mover));
    }

    // Announced straight into one of the customer's shipments.
    public Task<(OperationsPreAlert? PreAlert, OperationsParcel? Parcel, Error? Error)> AnnounceAsync(OperationsShipment shipment, OperationsPackageDraft draft, DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(shipment);
        return AnnounceAsync(shipment.TenantId, shipment.CustomerId, shipment.OriginWarehouseId, shipment, draft, utcNow, cancellationToken);
    }

    // Slice 4D: announced on its own for a warehouse, or into one of the customer's shipments from there (owner, 2026-10-19).
    public async Task<(OperationsPreAlert? PreAlert, OperationsParcel? Parcel, Error? Error)> AnnounceAsync(Guid tenantId, Guid customerId, Guid warehouseId, OperationsShipment? shipment,
        OperationsPackageDraft draft, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(draft);
        DateOnly today = DateOnly.FromDateTime(utcNow.UtcDateTime);

        if (draft.ExpectedDate is { } expected && (expected < today.AddDays(-30) || expected > today.AddDays(365)))
        {
            return (null, null, OperationsReceivingErrors.ExpectedDateInvalid);
        }

        string currency = (await _settings.FindAsync(cancellationToken))?.CurrencyCode ?? OperationsTenantSettings.DefaultCurrency;
        OperationsPreAlert preAlert;

        try
        {
            preAlert = shipment is null
                ? OperationsPreAlert.Announce(tenantId, customerId, warehouseId, draft.Carrier, draft.TrackingNumber, draft.Store, draft.Contents, draft.DeclaredValue, currency, draft.ExpectedDate, utcNow)
                : OperationsPreAlert.Create(shipment, draft.Carrier, draft.TrackingNumber, draft.Store, draft.Contents, draft.DeclaredValue, currency, draft.ExpectedDate, utcNow);
        }
        catch (DomainException exception)
        {
            return (null, null, exception.Error);
        }

        OperationsParcel? parcel = await _receiving.FindCustomerParcelByTrackingAsync(customerId, preAlert.TrackingNumber, cancellationToken);

        // Declared already: the parcel carries a declaration.
        if (parcel is { PreAlertId: not null })
        {
            return (null, null, OperationsReceivingErrors.PreAlertDuplicate);
        }

        // The package arrived at another warehouse: into a shipment from there only (review of 3C step 1). Announced on its own, it follows the parcel.
        if (parcel is not null && shipment is not null && parcel.WarehouseId != shipment.OriginWarehouseId)
        {
            return (null, null, OperationsShipmentErrors.ParcelElsewhere);
        }

        // One open announcement of a number per customer; one left in a shipment that closed gives way to this one.
        if (await _receiving.FindOpenPreAlertAsync(customerId, preAlert.TrackingNumber, cancellationToken) is { } earlier)
        {
            if (earlier.ShipmentId is not { } earlierShipment || await _receiving.GetShipmentAsync(earlierShipment, cancellationToken) is { Status: OperationsShipmentStatus.Open })
            {
                return (null, null, OperationsReceivingErrors.PreAlertDuplicate);
            }

            earlier.Cancel(utcNow);
        }

        // A cancel of the shipment at the same moment clashes on its row version.
        shipment?.TakePackage(utcNow);
        _receiving.Add(preAlert);

        if (parcel is null)
        {
            return (preAlert, null, null);
        }

        // The package came in first: the declaration answers the "awaiting information" hold, and the parcel goes where the pre-alert is.
        try
        {
            parcel.MatchPreAlert(preAlert, utcNow);
        }
        catch (DomainException exception)
        {
            return (null, null, exception.Error);
        }

        string key = "pal:" + preAlert.Id.ToString("N");
        List<OperationsTrackingDraft> steps = new()
        {
            new(OperationsTrackingSubject.Parcel, parcel.Id, OperationsTrackingCodes.PreAlerted, new Dictionary<string, string>(), utcNow, key + ":prealert",
                OperationsTrackingActor.Customer, OperationsTrackingSource.Customer)
        };

        if (parcel.HoldReasons.HasFlag(OperationsHoldReasons.AwaitingCustomerInformation))
        {
            steps.Add(new(OperationsTrackingSubject.Parcel, parcel.Id, OperationsTrackingCodes.HoldReleased,
                new Dictionary<string, string>(StringComparer.Ordinal) { ["reason"] = nameof(OperationsHoldReasons.AwaitingCustomerInformation) }, utcNow, key + ":release",
                OperationsTrackingActor.Customer, OperationsTrackingSource.Customer));
        }

        foreach (OperationsTrackingDraft step in steps)
        {
            if ((await _tracker.RecordAsync(step, cancellationToken)).Error is { } refused)
            {
                return (null, null, refused);
            }
        }

        // Announced on its own: a parcel already in one of the customer's shipments stays there, and one past its origin never moves (review of 4D).
        OperationsShipment? current = parcel.ShipmentId is { } currentId ? await _receiving.GetShipmentAsync(currentId, cancellationToken) : null;
        bool stays = shipment is null && (current is { HasInstructions: true, Status: OperationsShipmentStatus.Open } || parcel.Milestone > OperationsMilestone.ReceivedAtOrigin);

        if (stays)
        {
            if (current is { HasInstructions: true, Status: OperationsShipmentStatus.Open } kept)
            {
                try
                {
                    preAlert.PlaceIn(kept, utcNow);
                }
                catch (DomainException exception)
                {
                    return (null, null, exception.Error);
                }
            }
        }
        else if (await _mover.MoveParcelAsync(parcel, shipment, key + ":shipment", OperationsTrackingActor.Customer, OperationsTrackingSource.Customer, null, utcNow, cancellationToken,
            preAlert) is { } notMoved)
        {
            return (null, null, notMoved);
        }

        // Declared after its check at the station: the company's rules look at the declaration now (Slice 3C).
        if (shipment is null)
        {
            await _selection.RecheckAsync(parcel, null, utcNow, cancellationToken);
        }

        return (preAlert, parcel, null);
    }
}
