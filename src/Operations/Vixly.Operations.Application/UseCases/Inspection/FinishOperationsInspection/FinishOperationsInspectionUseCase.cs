using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Inspection;
using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Inspection;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Inspection.FinishOperationsInspection;

// =========================================================
// FILE: FinishOperationsInspectionUseCase.cs
//
// PURPOSE:
// Records what the inspector found and decided: passed, held for the customer, repacked or refused (Slice 3C design section 5).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.inspection.perform for the parcel’s warehouse
// - Photos are stored first and removed again when the save fails
// - Refused and disposed of waits for the Owner or an administrator (Q-C3)
// =========================================================
public sealed class FinishOperationsInspectionUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsInspectionRepository _inspections;

    private readonly IOperationsTrackingRepository _tracking;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsInspectionSteps _steps;

    private readonly OperationsInspectionPhotos _photos;

    private readonly OperationsOutboxWriter _outbox;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    private readonly Vixly.Operations.Application.Shipments.OperationsShipmentBooking _booking;

    public FinishOperationsInspectionUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsInspectionRepository inspections,
        IOperationsTrackingRepository tracking,
        IOperationsReceivingRepository receiving,
        IOperationsSettingsRepository settings,
        OperationsInspectionSteps steps,
        OperationsInspectionPhotos photos,
        OperationsOutboxWriter outbox,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        Vixly.Operations.Application.Shipments.OperationsShipmentBooking booking)
    {
        _booking = booking ?? throw new ArgumentNullException(nameof(booking));
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _inspections = inspections ?? throw new ArgumentNullException(nameof(inspections));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
        _photos = photos ?? throw new ArgumentNullException(nameof(photos));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<FinishOperationsInspectionResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        FinishOperationsInspectionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return FinishOperationsInspectionResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _inspections.GetInspectionAsync(request.InspectionId, cancellationToken) is not { } inspection
            || await _scope.CoveredAsync(access, OperationsPermissions.InspectionPerform, inspection.WarehouseId, cancellationToken) is not { } warehouse
            || await _tracking.GetParcelAsync(inspection.ParcelId, cancellationToken) is not { } parcel)
        {
            return FinishOperationsInspectionResponse.Failure(OperationsInspectionErrors.NotFound);
        }

        if (!OperationsCommandKey.IsValid(request.CommandKey))
        {
            return FinishOperationsInspectionResponse.Failure(OperationsTrackingErrors.IdempotencyKeyInvalid);
        }

        string key = "ins:" + request.CommandKey;

        // Sent twice (a double click, a retry): the first one counted.
        if (await _tracking.FindStoredByKeyAsync(key + ":outcome", cancellationToken) is { } earlier && earlier.SubjectId == parcel.Id)
        {
            return FinishOperationsInspectionResponse.Success(inspection.Result ?? request.Result);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        IReadOnlyList<OperationsRestrictedItem> listed = await _inspections.ListItemsAsync(cancellationToken);
        List<string> found = request.ItemIds.Distinct().Select(id => listed.FirstOrDefault(item => item.Id == id)?.Name).OfType<string>().ToList();
        string currency = (await _settings.FindAsync(cancellationToken))?.CurrencyCode ?? OperationsTenantSettings.DefaultCurrency;

        try
        {
            inspection.Finish(access.User.Id, new OperationsInspectionOutcome(request.Result, found, request.Notes, request.CustomerAction, request.HoldReason, request.Fee,
                request.CalledCustomerUtc, request.CallNotes), currency, utcNow);
        }
        catch (DomainException exception)
        {
            return FinishOperationsInspectionResponse.Failure(exception.Error);
        }

        (IReadOnlyList<(OperationsParcelPhotoUpload Upload, OperationsImageInfo Info)>? photos, Error? invalid) = _photos.Check(request.Photos);

        if (photos is null)
        {
            return FinishOperationsInspectionResponse.Failure(invalid!);
        }

        // The outcome (ADR-0022): a disposal waits for approval with the parcel refused and on hold; held and refused parcels wait on hold (design section 5).
        bool disposal = inspection.Status == OperationsInspectionStatus.AwaitingApproval;
        List<(string Key, string Code, string? Argument, string? Value, string? Note)> steps = new();

        for (int index = 0; index < photos.Count; index++)
        {
            steps.Add((key + ":photo:" + index, OperationsTrackingCodes.PhotoAdded, "kind", "contents", null));
        }

        steps.Add(disposal
            ? (key + ":outcome", OperationsTrackingCodes.HoldOpened, OperationsTrackingCode.ReasonArgument, nameof(OperationsHoldReasons.Refused), null)
            : (key + ":outcome", OperationsTrackingCodes.InspectionCompleted, "result", inspection.Result, null));

        OperationsHoldReasons hold = inspection.Result == "held" ? inspection.HoldReason : inspection.Result == "refused_returned" ? OperationsHoldReasons.Refused : OperationsHoldReasons.None;

        if (hold != OperationsHoldReasons.None && !parcel.HoldReasons.HasFlag(hold))
        {
            steps.Add((key + ":hold", OperationsTrackingCodes.HoldOpened, OperationsTrackingCode.ReasonArgument, hold.ToString(), null));
        }

        List<string> stored = new();
        bool saved = false;

        try
        {
            stored.AddRange(await _photos.StoreAsync(context.RequiredTenantId, parcel.Id, photos, access.User.Id, utcNow, cancellationToken));

            if (await _steps.RecordAsync(parcel.Id, access, warehouse, utcNow, cancellationToken, steps.ToArray()) is { } refused)
            {
                return FinishOperationsInspectionResponse.Failure(refused);
            }

            // The customer is told by email what to do (the portal answer comes with 3E).
            if (parcel.CustomerId is { } customerId && (inspection.Result == "held" || inspection.Result!.StartsWith("refused", StringComparison.Ordinal)))
            {
                _outbox.Write(OperationsReceivingMessages.ParcelNotice, context.RequiredTenantId, parcel.Id,
                    new OperationsParcelNotice(customerId, parcel.Id, OperationsCustomerMailKind.ParcelActionNeeded, request.PortalSlug));
            }

            // Slice 5C: the inspection passed, its shipment may now be final (C-2).
            if (parcel.ShipmentId is { } shipmentId && await _receiving.GetShipmentAsync(shipmentId, cancellationToken) is { } shipment)
            {
                await _booking.TryBookAsync(shipment, OperationsTrackingActor.Staff, access.User.Id, warehouse.Code, OperationsTrackingSource.Manual, utcNow, cancellationToken);
            }

            _audit.Record(access.User.Id, "ops.inspection.finished", nameof(OperationsInspection), inspection.Id,
                after: new { parcel = parcel.Number, result = inspection.Result, items = inspection.ItemsFound, hold = inspection.HoldReason.ToString(), fee = inspection.Fee });
            saved = await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            if (!saved)
            {
                await _photos.DeleteAsync(context.RequiredTenantId, stored);
            }
        }

        if (saved)
        {
            return FinishOperationsInspectionResponse.Success(inspection.Result!);
        }

        // Two inspectors at once, or the same form twice: the row version keeps the first.
        _unitOfWork.DiscardChanges();
        return await _tracking.FindStoredByKeyAsync(key + ":outcome", cancellationToken) is { } raced && raced.SubjectId == parcel.Id
            ? FinishOperationsInspectionResponse.Success(request.Result)
            : FinishOperationsInspectionResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
