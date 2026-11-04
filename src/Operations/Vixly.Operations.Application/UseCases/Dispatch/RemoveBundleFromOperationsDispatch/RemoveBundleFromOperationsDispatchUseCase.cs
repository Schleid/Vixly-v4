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

namespace Vixly.Operations.Application.UseCases.Dispatch.RemoveBundleFromOperationsDispatch;

// =========================================================
// FILE: RemoveBundleFromOperationsDispatchUseCase.cs
//
// PURPOSE:
// Take a bundle off a planned dispatch (Slice 6B, D-3).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.dispatches.manage at the dispatch's origin warehouse (D-8)
// =========================================================
public sealed class RemoveBundleFromOperationsDispatchUseCase
{
    private readonly OperationsBundleDesk _desk;

    private readonly IOperationsDispatchRepository _dispatches;

    private readonly IOperationsBundleRepository _bundles;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public RemoveBundleFromOperationsDispatchUseCase(
        OperationsBundleDesk desk,
        IOperationsDispatchRepository dispatches,
        IOperationsBundleRepository bundles,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _dispatches = dispatches ?? throw new ArgumentNullException(nameof(dispatches));
        _bundles = bundles ?? throw new ArgumentNullException(nameof(bundles));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<RemoveBundleFromOperationsDispatchResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        RemoveBundleFromOperationsDispatchRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, new[] { OperationsPermissions.DispatchesManage }, cancellationToken);

        if (refused is not null)
        {
            return RemoveBundleFromOperationsDispatchResponse.Failure(refused);
        }

        if (await _dispatches.GetDispatchAsync(request.DispatchId, cancellationToken) is not { } dispatch || warehouses.All(item => item.Id != dispatch.WarehouseId))
        {
            return RemoveBundleFromOperationsDispatchResponse.Failure(OperationsDispatchErrors.NotFound);
        }

        if (await _bundles.GetBundleAsync(request.BundleId, cancellationToken) is not { } bundle)
        {
            return RemoveBundleFromOperationsDispatchResponse.Failure(OperationsBundleErrors.NotFound);
        }

        // Off it already (a double click): done.
        if (bundle.DispatchId != dispatch.Id)
        {
            return RemoveBundleFromOperationsDispatchResponse.Success();
        }

        try
        {
            dispatch.ReleaseBundle(bundle, _timeProvider.GetUtcNow());
            await _dispatches.RemoveLinkAsync(dispatch.Id, bundle.Id, cancellationToken);
        }
        catch (DomainException exception)
        {
            return RemoveBundleFromOperationsDispatchResponse.Failure(exception.Error);
        }

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return RemoveBundleFromOperationsDispatchResponse.Success();
        }

        _unitOfWork.DiscardChanges();
        return RemoveBundleFromOperationsDispatchResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
