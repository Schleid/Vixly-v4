using Vixly.Operations.Application.Inspection;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.CheckOperationsParcel;

// =========================================================
// FILE: CheckOperationsParcelUseCase.cs
//
// PURPOSE:
// Records a parcel at the inspection station: condition, weight, size and photos; picks some for compliance inspection (Slice 3B, owner 2026-10-18).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - A second check corrects weight and size; the random draw happens only the first time
// - Weight and size are typed in the company's units; the scale and dimensioner plug into the same fields later
// =========================================================
public sealed class CheckOperationsParcelUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsTrackingRepository _tracking;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsTracker _tracker;

    private readonly IOperationsImageInspector _images;

    private readonly IOperationsFileStore _files;

    private readonly OperationsInspectionSelection _selection;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    private readonly Vixly.Operations.Application.Printing.OperationsLabelPrinting _labels;

    private readonly Vixly.Operations.Application.Printing.OperationsShippingLabels _shippingLabels;

    private readonly Vixly.Operations.Application.Shipments.OperationsShipmentBooking _booking;

    public CheckOperationsParcelUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsReceivingRepository receiving,
        IOperationsTrackingRepository tracking,
        IOperationsSettingsRepository settings,
        OperationsTracker tracker,
        IOperationsImageInspector images,
        IOperationsFileStore files,
        OperationsInspectionSelection selection,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        Vixly.Operations.Application.Printing.OperationsLabelPrinting labels,
        Vixly.Operations.Application.Printing.OperationsShippingLabels shippingLabels,
        Vixly.Operations.Application.Shipments.OperationsShipmentBooking booking)
    {
        _booking = booking ?? throw new ArgumentNullException(nameof(booking));
        _labels = labels ?? throw new ArgumentNullException(nameof(labels));
        _shippingLabels = shippingLabels ?? throw new ArgumentNullException(nameof(shippingLabels));
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        _images = images ?? throw new ArgumentNullException(nameof(images));
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _selection = selection ?? throw new ArgumentNullException(nameof(selection));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<CheckOperationsParcelResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        CheckOperationsParcelRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return CheckOperationsParcelResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _tracking.GetParcelAsync(request.ParcelId, cancellationToken) is not { WarehouseId: { } warehouseId } parcel
            || await _scope.CoveredAsync(access, OperationsPermissions.ParcelsView, warehouseId, cancellationToken) is not { } warehouse)
        {
            return CheckOperationsParcelResponse.Failure(OperationsReceivingErrors.ParcelNotFound);
        }

        if (!access.CoversWarehouse(OperationsPermissions.InspectionCheck, warehouse.Id, warehouse.BranchId))
        {
            return CheckOperationsParcelResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        if (!OperationsCommandKey.IsValid(request.CommandKey))
        {
            return CheckOperationsParcelResponse.Failure(OperationsTrackingErrors.IdempotencyKeyInvalid);
        }

        string key = "chk:" + request.CommandKey;

        if (await _tracking.FindStoredByKeyAsync(key + ":checked", cancellationToken) is { } earlier && earlier.SubjectId == parcel.Id)
        {
            return CheckOperationsParcelResponse.Success(parcel.Id, parcel.Flags.HasFlag(OperationsParcelFlags.InspectionPending));
        }

        if (!parcel.IsAtOrigin)
        {
            return CheckOperationsParcelResponse.Failure(OperationsReceivingErrors.ParcelClosed);
        }

        // Slice 5C: a parcel of a final shipment keeps the weight and size it was priced with (C-3).
        if (parcel.Milestone >= OperationsMilestone.Booked)
        {
            return CheckOperationsParcelResponse.Failure(Vixly.Operations.Domain.Shipments.OperationsShipmentErrors.Booked);
        }

        if (!OperationsTrackingCodes.Conditions.Contains(request.Condition, StringComparer.Ordinal))
        {
            return CheckOperationsParcelResponse.Failure(OperationsReceivingErrors.ConditionInvalid);
        }

        List<(OperationsParcelPhotoUpload Upload, OperationsImageInfo Info)> photos = new();

        foreach (OperationsParcelPhotoUpload upload in request.Photos)
        {
            if (!OperationsTrackingCodes.PhotoKinds.Contains(upload.Kind, StringComparer.Ordinal) || upload.Content.Length is 0 or > OperationsReceivingLimits.MaxPhotoBytes
                || _images.Inspect(upload.Content) is not { } info)
            {
                return CheckOperationsParcelResponse.Failure(OperationsReceivingErrors.PhotoInvalid);
            }

            photos.Add((upload, info));
        }

        string? flagReason = null;

        try
        {
            flagReason = request.Flag
                ? Vixly.Operations.Domain.Inspection.OperationsDeclarationText.Required(request.FlagReason, Vixly.Operations.Domain.Inspection.OperationsInspectionLimits.ReasonMaxLength,
                    Vixly.Operations.Domain.Inspection.OperationsInspectionErrors.FlagReasonRequired)
                : null;
        }
        catch (DomainException exception)
        {
            return CheckOperationsParcelResponse.Failure(exception.Error);
        }

        // A flag on a parcel already chosen or inspected is refused, never dropped silently (review of 3C).
        if (flagReason is not null && await _selection.WasChosenAsync(parcel, cancellationToken))
        {
            return CheckOperationsParcelResponse.Failure(Vixly.Operations.Domain.Inspection.OperationsInspectionErrors.AlreadyChosen);
        }

        // A photo of the package the first time; a damage photo for anything that is not intact (ADR-0022 condition check).
        bool first = parcel.Flags.HasFlag(OperationsParcelFlags.AwaitingCheck);

        if (photos.Count > OperationsReceivingLimits.MaxPhotosPerReceipt
            || (first && !photos.Any(photo => photo.Upload.Kind == "package"))
            || (request.Condition != "intact" && !photos.Any(photo => photo.Upload.Kind == "damage")))
        {
            return CheckOperationsParcelResponse.Failure(OperationsReceivingErrors.PhotoRequired);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, utcNow);
        OperationsMeasurements measurements;

        try
        {
            measurements = OperationsMeasurements.Create(
                OperationsUnits.ToKilograms(request.Weight, settings.WeightUnit),
                OperationsUnits.ToCentimetres(request.Length, settings.DimensionUnit),
                OperationsUnits.ToCentimetres(request.Width, settings.DimensionUnit),
                OperationsUnits.ToCentimetres(request.Height, settings.DimensionUnit));
        }
        catch (DomainException exception)
        {
            return CheckOperationsParcelResponse.Failure(exception.Error);
        }

        parcel.Measure(measurements, utcNow);
        List<string> stored = new();
        bool saved = false;

        try
        {
            foreach ((OperationsParcelPhotoUpload upload, OperationsImageInfo info) in photos)
            {
                string storageKey = await _files.SaveAsync(context.RequiredTenantId, "parcels", upload.Content, info.ContentType, cancellationToken);
                stored.Add(storageKey);
                _receiving.Add(OperationsParcelPhoto.Add(context.RequiredTenantId, parcel.Id, upload.Kind, storageKey, info.ContentType, upload.Content.Length, info.Sha256, access.User.Id, utcNow));
            }

            List<OperationsTrackingDraft> steps = new() { Step(key + ":condition", parcel, OperationsTrackingCodes.ConditionRecorded, access, warehouse, utcNow, ("condition", request.Condition)) };

            for (int index = 0; index < photos.Count; index++)
            {
                steps.Add(Step(key + ":photo:" + index, parcel, OperationsTrackingCodes.PhotoAdded, access, warehouse, utcNow, ("kind", photos[index].Upload.Kind)));
            }

            if (request.Condition != "intact" && !parcel.HoldReasons.HasFlag(OperationsHoldReasons.Damaged))
            {
                steps.Add(Step(key + ":hold:damaged", parcel, OperationsTrackingCodes.HoldOpened, access, warehouse, utcNow, ("reason", nameof(OperationsHoldReasons.Damaged))));
            }

            steps.Add(Step(key + ":checked", parcel, OperationsTrackingCodes.Checked, access, warehouse, utcNow,
                ("weight", measurements.WeightKg.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)),
                ("size", string.Join(" x ", new[] { measurements.LengthCm, measurements.WidthCm, measurements.HeightCm }.Select(side => side.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture))))));

            foreach (OperationsTrackingDraft step in steps)
            {
                if ((await _tracker.RecordAsync(step, cancellationToken)).Error is { } refused)
                {
                    return CheckOperationsParcelResponse.Failure(refused);
                }
            }

            // Compliance inspection: the clerk's flag, the company's rules, or its random share the first time (design section 4).
            bool selected = parcel.Flags.HasFlag(OperationsParcelFlags.InspectionPending);

            if (!selected && await _selection.ChooseAsync(parcel, first, flagReason, cancellationToken) is { } choice)
            {
                selected = await _selection.OpenAsync(parcel, choice, access.User.Id, key + ":inspection", OperationsTrackingActor.Staff, OperationsTrackingSource.Scan, warehouse.Code, utcNow,
                    cancellationToken) is not null;
            }

            // Slice 5C: its shipment may now have all its parcels measured and clear: it becomes final (C-2).
            if (parcel.ShipmentId is { } shipmentId && await _receiving.GetShipmentAsync(shipmentId, cancellationToken) is { } shipment)
            {
                await _booking.TryBookAsync(shipment, OperationsTrackingActor.Staff, access.User.Id, warehouse.Code, OperationsTrackingSource.Scan, utcNow, cancellationToken);
            }

            // Slice 4D step 2: cleared now, its shipping label prints at the station and is stuck on (A-5); otherwise it waits in the labels to print.
            bool labelled = false;

            if (await _labels.UsableAsync(request.ShippingPrinterId, warehouse.Id, Vixly.Operations.Domain.Printing.OperationsLabelMedia.ShippingLabel, cancellationToken) is { } shippingPrinter
                && await _shippingLabels.BuildAsync(parcel, cancellationToken) is { } label)
            {
                _labels.QueueShippingLabel(context.RequiredTenantId, shippingPrinter, label);
                parcel.ShippingLabelPrinted(utcNow);
                labelled = true;
            }

            saved = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (saved)
            {
                return CheckOperationsParcelResponse.Success(parcel.Id, selected, labelled);
            }
        }
        finally
        {
            if (!saved)
            {
                foreach (string storageKey in stored)
                {
                    await _files.DeleteAsync(context.RequiredTenantId, storageKey, CancellationToken.None);
                }
            }
        }

        _unitOfWork.DiscardChanges();
        return await _tracking.FindStoredByKeyAsync(key + ":checked", cancellationToken) is { } raced && raced.SubjectId == parcel.Id
            ? CheckOperationsParcelResponse.Success(parcel.Id, false)
            : CheckOperationsParcelResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }

    private static OperationsTrackingDraft Step(string key, OperationsParcel parcel, string code, OperationsAccessView access, OperationsWarehouse warehouse, DateTimeOffset utcNow,
        params (string Name, string Value)[] arguments) =>
        new(OperationsTrackingSubject.Parcel, parcel.Id, code, arguments.ToDictionary(item => item.Name, item => item.Value, StringComparer.Ordinal), utcNow, key,
            OperationsTrackingActor.Staff, OperationsTrackingSource.Scan)
        {
            ActorUserId = access.User.Id,
            LocationCode = warehouse.Code
        };
    
}
