using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.UseCases.Receiving.RecordOperationsParcelLabel;

// =========================================================
// FILE: RecordOperationsParcelLabelUseCase.cs
//
// PURPOSE:
// Records what the label of an undeclared package shows: the name and mailbox code written on it, the sender (Slice 4D step 3, design section 6).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.parcels.assign for the parcel's warehouse; only an unidentified package still at its warehouse
// - Staff read the label; a customer is never matched by name alone (Q-R1)
// =========================================================
public sealed class RecordOperationsParcelLabelUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsTrackingRepository _tracking;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    private readonly Vixly.Operations.Application.Tracking.OperationsTracker _tracker;

    public RecordOperationsParcelLabelUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsTrackingRepository tracking,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        Vixly.Operations.Application.Tracking.OperationsTracker tracker)
    {
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<RecordOperationsParcelLabelResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        RecordOperationsParcelLabelRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return RecordOperationsParcelLabelResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _tracking.GetParcelAsync(request.ParcelId, cancellationToken) is not { WarehouseId: { } warehouseId } parcel
            || await _scope.CoveredAsync(access, OperationsPermissions.ParcelsView, warehouseId, cancellationToken) is not { } warehouse)
        {
            return RecordOperationsParcelLabelResponse.Failure(OperationsReceivingErrors.ParcelNotFound);
        }

        if (!access.CoversWarehouse(OperationsPermissions.ParcelsAssign, warehouse.Id, warehouse.BranchId))
        {
            return RecordOperationsParcelLabelResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        // Only a package nobody could place, still here.
        if (parcel.IsIdentified || !parcel.IsAtOrigin)
        {
            return RecordOperationsParcelLabelResponse.Failure(OperationsReceivingErrors.NotUnidentified);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        try
        {
            parcel.RecordLabel(request.Name, request.Code, request.Sender, utcNow);
        }
        catch (DomainException exception)
        {
            return RecordOperationsParcelLabelResponse.Failure(exception.Error);
        }

        // Every step is a tracking event (ADR-0021); the words themselves stay in the audit.
        if ((await _tracker.RecordAsync(new Vixly.Operations.Domain.Tracking.OperationsTrackingDraft(Vixly.Operations.Domain.Tracking.OperationsTrackingSubject.Parcel, parcel.Id,
            Vixly.Operations.Domain.Tracking.OperationsTrackingCodes.LabelRecorded, new Dictionary<string, string>(), utcNow, "lbl:" + parcel.Id.ToString("N") + ":" + utcNow.UtcTicks,
            Vixly.Operations.Domain.Tracking.OperationsTrackingActor.Staff, Vixly.Operations.Domain.Tracking.OperationsTrackingSource.Manual)
            {
                ActorUserId = access.User.Id,
                LocationCode = warehouse.Code
            }, cancellationToken)).Error is { } refused)
        {
            return RecordOperationsParcelLabelResponse.Failure(refused);
        }

        _audit.Record(access.User.Id, "ops.parcels.label_recorded", nameof(OperationsParcel), parcel.Id,
            after: new { name = parcel.LabelName, code = parcel.LabelCode, sender = parcel.LabelSender });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? RecordOperationsParcelLabelResponse.Success()
            : RecordOperationsParcelLabelResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
