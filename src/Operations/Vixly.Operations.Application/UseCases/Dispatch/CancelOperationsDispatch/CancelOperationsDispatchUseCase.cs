using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Application.Dispatch;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Dispatch.CancelOperationsDispatch;

// =========================================================
// FILE: CancelOperationsDispatchUseCase.cs
//
// PURPOSE:
// Cancel a dispatch that has not left, with a reason (Slice 6B, D-2).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.dispatches.manage at the dispatch's origin warehouse (D-8)
// - Once closed, the parcels' manifested events are voided
// =========================================================
public sealed class CancelOperationsDispatchUseCase
{
    private readonly OperationsBundleDesk _desk;

    private readonly IOperationsDispatchRepository _dispatches;

    private readonly OperationsDispatching _dispatching;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public CancelOperationsDispatchUseCase(
        OperationsBundleDesk desk,
        IOperationsDispatchRepository dispatches,
        OperationsDispatching dispatching,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _dispatches = dispatches ?? throw new ArgumentNullException(nameof(dispatches));
        _dispatching = dispatching ?? throw new ArgumentNullException(nameof(dispatching));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<CancelOperationsDispatchResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        CancelOperationsDispatchRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, new[] { OperationsPermissions.DispatchesManage }, cancellationToken);

        if (refused is not null)
        {
            return CancelOperationsDispatchResponse.Failure(refused);
        }

        if (await _dispatches.GetDispatchAsync(request.DispatchId, cancellationToken) is not { } dispatch || warehouses.All(item => item.Id != dispatch.WarehouseId))
        {
            return CancelOperationsDispatchResponse.Failure(OperationsDispatchErrors.NotFound);
        }

        // Cancelled already (a double click): done.
        if (dispatch.Status == OperationsDispatchStatus.Cancelled)
        {
            return CancelOperationsDispatchResponse.Success();
        }

        bool wasClosed = dispatch.Status == OperationsDispatchStatus.Closed;
        IReadOnlyList<OperationsBundle> bundles = await _dispatches.ListDispatchBundlesAsync(dispatch.Id, cancellationToken);
        IReadOnlyList<Vixly.Operations.Domain.Tracking.OperationsParcel> parcels = wasClosed ? await _dispatching.ParcelsAsync(dispatch, cancellationToken) : Array.Empty<Vixly.Operations.Domain.Tracking.OperationsParcel>();
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        try
        {
            dispatch.Cancel(request.Reason, utcNow);
        }
        catch (DomainException exception)
        {
            return CancelOperationsDispatchResponse.Failure(exception.Error);
        }

        // Its bundles are free again.
        foreach (OperationsBundle bundle in bundles)
        {
            OperationsDispatch.Free(bundle, utcNow);
            await _dispatches.RemoveLinkAsync(dispatch.Id, bundle.Id, cancellationToken);
        }

        await _dispatching.CancelledAsync(dispatch, parcels, wasClosed, access!.User.Id, cancellationToken);

        // Its shipments' house numbers go with it.
        foreach (OperationsDispatchShipment row in await _dispatches.ListDispatchShipmentsAsync(dispatch.Id, cancellationToken))
        {
            _dispatches.Remove(row);
        }

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return CancelOperationsDispatchResponse.Success();
        }

        _unitOfWork.DiscardChanges();
        return CancelOperationsDispatchResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
