using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.PutAwayOperationsParcel;

// =========================================================
// FILE: PutAwayOperationsParcelUseCase.cs
//
// PURPOSE:
// Puts a parcel on a shelf of its warehouse (Slice 3B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.receiving.receive for the parcel's warehouse; the shelf must be active and in that warehouse
// =========================================================
public sealed class PutAwayOperationsParcelUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsTrackingRepository _tracking;

    private readonly OperationsTracker _tracker;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public PutAwayOperationsParcelUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsReceivingRepository receiving,
        IOperationsTrackingRepository tracking,
        OperationsTracker tracker,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<PutAwayOperationsParcelResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        PutAwayOperationsParcelRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return PutAwayOperationsParcelResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _tracking.GetParcelAsync(request.ParcelId, cancellationToken) is not { WarehouseId: { } warehouseId } parcel
            || await _scope.CoveredAsync(access, OperationsPermissions.ParcelsView, warehouseId, cancellationToken) is not { } warehouse)
        {
            return PutAwayOperationsParcelResponse.Failure(OperationsReceivingErrors.ParcelNotFound);
        }

        if (!access.CoversWarehouse(OperationsPermissions.ReceivingReceive, warehouse.Id, warehouse.BranchId))
        {
            return PutAwayOperationsParcelResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        if (!OperationsCommandKey.IsValid(request.CommandKey))
        {
            return PutAwayOperationsParcelResponse.Failure(OperationsTrackingErrors.IdempotencyKeyInvalid);
        }

        string key = "put:" + request.CommandKey;

        if (await _tracking.FindStoredByKeyAsync(key, cancellationToken) is { } earlier && earlier.SubjectId == parcel.Id)
        {
            return PutAwayOperationsParcelResponse.Success(parcel.Id);
        }

        // Only while the parcel is at its origin warehouse (review L4).
        if (!parcel.IsAtOrigin)
        {
            return PutAwayOperationsParcelResponse.Failure(OperationsReceivingErrors.ParcelClosed);
        }

        if (await _receiving.GetLocationAsync(request.StorageLocationId, cancellationToken) is not { IsActive: true } location || location.WarehouseId != warehouse.Id)
        {
            return PutAwayOperationsParcelResponse.Failure(OperationsReceivingErrors.LocationInactive);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        parcel.PutAway(location, utcNow);
        OperationsTrackingDraft draft = new(OperationsTrackingSubject.Parcel, parcel.Id, OperationsTrackingCodes.PutAway,
            new Dictionary<string, string>(StringComparer.Ordinal) { ["location"] = location.Code }, utcNow, key, OperationsTrackingActor.Staff, OperationsTrackingSource.Manual)
        {
            ActorUserId = access.User.Id,
            LocationCode = warehouse.Code
        };

        if ((await _tracker.RecordAsync(draft, cancellationToken)).Error is { } refused)
        {
            return PutAwayOperationsParcelResponse.Failure(refused);
        }

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return PutAwayOperationsParcelResponse.Success(parcel.Id);
        }

        // The same form sent twice at once: the other request saved it (ADR-0019, review M2).
        _unitOfWork.DiscardChanges();
        return await _tracking.FindStoredByKeyAsync(key, cancellationToken) is { } raced && raced.SubjectId == parcel.Id
            ? PutAwayOperationsParcelResponse.Success(parcel.Id)
            : PutAwayOperationsParcelResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
