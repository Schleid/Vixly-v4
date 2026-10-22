using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Inspection;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Inspection;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Inspection.ReleaseOperationsHold;

// =========================================================
// FILE: ReleaseOperationsHoldUseCase.cs
//
// PURPOSE:
// Releases a hold with a note, for example "customer sent the invoice by email" (Slice 3C design section 6).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.holds.manage for the parcel's warehouse
// - The history records who released it and the note (ADR-0021)
// =========================================================
public sealed class ReleaseOperationsHoldUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsTrackingRepository _tracking;

    private readonly OperationsInspectionSteps _steps;

    private readonly IOperationsInspectionRepository _inspections;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly Vixly.Operations.Application.Shipments.OperationsShipmentBooking _booking;

    public ReleaseOperationsHoldUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsTrackingRepository tracking,
        OperationsInspectionSteps steps,
        IOperationsInspectionRepository inspections,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IOperationsReceivingRepository receiving,
        Vixly.Operations.Application.Shipments.OperationsShipmentBooking booking)
    {
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _booking = booking ?? throw new ArgumentNullException(nameof(booking));
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
        _inspections = inspections ?? throw new ArgumentNullException(nameof(inspections));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ReleaseOperationsHoldResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ReleaseOperationsHoldRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return ReleaseOperationsHoldResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _tracking.GetParcelAsync(request.ParcelId, cancellationToken) is not { WarehouseId: { } warehouseId } parcel
            || await _scope.CoveredAsync(access, OperationsPermissions.HoldsManage, warehouseId, cancellationToken) is not { } warehouse)
        {
            return ReleaseOperationsHoldResponse.Failure(OperationsReceivingErrors.ParcelNotFound);
        }

        if (!OperationsCommandKey.IsValid(request.CommandKey))
        {
            return ReleaseOperationsHoldResponse.Failure(OperationsTrackingErrors.IdempotencyKeyInvalid);
        }

        string key = "rel:" + request.CommandKey;

        if (await _tracking.FindStoredByKeyAsync(key, cancellationToken) is { } earlier && earlier.SubjectId == parcel.Id)
        {
            return ReleaseOperationsHoldResponse.Success();
        }

        // Unidentified ends by assigning a customer, awaiting information when the customer answers.
        // Refused ends when a parcel refused for return leaves: "returned to sender" (review of 3C); a disposal ends by its approval.
        bool returned = request.Reason == OperationsHoldReasons.Refused
            && await _inspections.FindLatestAsync(parcel.Id, cancellationToken) is { Result: "refused_returned", Status: OperationsInspectionStatus.Done };

        if ((!Releasable.Contains(request.Reason) && !returned) || !parcel.HoldReasons.HasFlag(request.Reason))
        {
            return ReleaseOperationsHoldResponse.Failure(OperationsInspectionErrors.HoldNotReleasable);
        }

        string note;

        try
        {
            note = OperationsDeclarationText.Required(request.Note, OperationsInspectionLimits.CallNotesMaxLength, OperationsInspectionErrors.ReleaseNoteRequired);
        }
        catch (DomainException exception)
        {
            return ReleaseOperationsHoldResponse.Failure(exception.Error);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        if (returned && await _steps.RecordAsync(parcel.Id, access, warehouse, utcNow, cancellationToken,
            (key + ":returned", OperationsTrackingCodes.ReturnedToSender, null, null, note)) is { } notReturned)
        {
            return ReleaseOperationsHoldResponse.Failure(notReturned);
        }

        if (await _steps.RecordAsync(parcel.Id, access, warehouse, utcNow, cancellationToken,
            (key, OperationsTrackingCodes.HoldReleased, OperationsTrackingCode.ReasonArgument, request.Reason.ToString(), note)) is { } refused)
        {
            return ReleaseOperationsHoldResponse.Failure(refused);
        }

        _audit.Record(access.User.Id, "ops.holds.released", nameof(OperationsParcel), parcel.Id, after: new { parcel = parcel.Number, reason = request.Reason.ToString(), note });

        // Slice 5C: the last hold of its shipment released, the shipment may now be final (C-2).
        if (parcel.ShipmentId is { } shipmentId && await _receiving.GetShipmentAsync(shipmentId, cancellationToken) is { } shipment)
        {
            await _booking.TryBookAsync(shipment, OperationsTrackingActor.Staff, access.User.Id, warehouse.Code, OperationsTrackingSource.Manual, utcNow, cancellationToken);
        }

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return ReleaseOperationsHoldResponse.Success();
        }

        // The same release sent twice at once: the other one was saved.
        _unitOfWork.DiscardChanges();
        return await _tracking.FindStoredByKeyAsync(key, cancellationToken) is { } raced && raced.SubjectId == parcel.Id
            ? ReleaseOperationsHoldResponse.Success()
            : ReleaseOperationsHoldResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }

    // The holds a person may release by hand.
    public static IReadOnlyList<OperationsHoldReasons> Releasable { get; } = new[]
    {
        OperationsHoldReasons.Damaged, OperationsHoldReasons.InspectionDocuments, OperationsHoldReasons.InspectionPermit, OperationsHoldReasons.InspectionValue,
        OperationsHoldReasons.InspectionFee, OperationsHoldReasons.Other
    };
    
}
