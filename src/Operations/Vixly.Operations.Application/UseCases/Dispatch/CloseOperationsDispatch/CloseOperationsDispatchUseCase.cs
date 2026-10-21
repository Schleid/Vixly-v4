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

namespace Vixly.Operations.Application.UseCases.Dispatch.CloseOperationsDispatch;

// =========================================================
// FILE: CloseOperationsDispatchUseCase.cs
//
// PURPOSE:
// Close (manifest) a dispatch with its bundles and legs (Slice 6B, D-2 and D-5).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.dispatches.manage at the dispatch's origin warehouse (D-8)
// - Each parcel is manifested (internal); the dispatch's bundle count is checked by its row version
// =========================================================
public sealed class CloseOperationsDispatchUseCase
{
    private readonly OperationsBundleDesk _desk;

    private readonly IOperationsDispatchRepository _dispatches;

    private readonly OperationsDispatching _dispatching;

    private readonly OperationsDispatchPapers _papers;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public CloseOperationsDispatchUseCase(
        OperationsBundleDesk desk,
        IOperationsDispatchRepository dispatches,
        OperationsDispatching dispatching,
        OperationsDispatchPapers papers,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _dispatches = dispatches ?? throw new ArgumentNullException(nameof(dispatches));
        _dispatching = dispatching ?? throw new ArgumentNullException(nameof(dispatching));
        _papers = papers ?? throw new ArgumentNullException(nameof(papers));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<CloseOperationsDispatchResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        CloseOperationsDispatchRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, new[] { OperationsPermissions.DispatchesManage }, cancellationToken);

        if (refused is not null)
        {
            return CloseOperationsDispatchResponse.Failure(refused);
        }

        if (await _dispatches.GetDispatchAsync(request.DispatchId, cancellationToken) is not { } dispatch || warehouses.All(item => item.Id != dispatch.WarehouseId))
        {
            return CloseOperationsDispatchResponse.Failure(OperationsDispatchErrors.NotFound);
        }

        // Closed already (a double click): done.
        if (dispatch.Status != OperationsDispatchStatus.Planned && dispatch.ClosedUtc is not null)
        {
            return CloseOperationsDispatchResponse.Success();
        }

        IReadOnlyList<Vixly.Operations.Domain.Tracking.OperationsParcel> parcels = await _dispatching.ParcelsAsync(dispatch, cancellationToken);

        // A parcel put on hold after its bundle closed stops the dispatch (review).
        if (parcels.Any(OperationsDispatching.Held))
        {
            return CloseOperationsDispatchResponse.Failure(OperationsDispatchErrors.ParcelHeld);
        }

        try
        {
            dispatch.Close(_timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return CloseOperationsDispatchResponse.Failure(exception.Error);
        }

        await _dispatching.ClosedAsync(dispatch, parcels, access!.User.Id, cancellationToken);

        // Step 2: one row per shipment with its house number (D-6).
        await _papers.ManifestAsync(dispatch, parcels, _timeProvider.GetUtcNow(), cancellationToken);

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return CloseOperationsDispatchResponse.Success();
        }

        _unitOfWork.DiscardChanges();
        return CloseOperationsDispatchResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
