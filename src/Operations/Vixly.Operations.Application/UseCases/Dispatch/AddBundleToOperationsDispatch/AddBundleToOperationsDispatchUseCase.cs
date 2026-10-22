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

namespace Vixly.Operations.Application.UseCases.Dispatch.AddBundleToOperationsDispatch;

// =========================================================
// FILE: AddBundleToOperationsDispatchUseCase.cs
//
// PURPOSE:
// Put a closed bundle on a planned dispatch, chosen or scanned (Slice 6B, D-3).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.dispatches.manage at the dispatch's origin warehouse (D-8)
// =========================================================
public sealed class AddBundleToOperationsDispatchUseCase
{
    private readonly OperationsBundleDesk _desk;

    private readonly IOperationsDispatchRepository _dispatches;

    private readonly IOperationsBundleRepository _bundles;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public AddBundleToOperationsDispatchUseCase(
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

    public async Task<AddBundleToOperationsDispatchResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        AddBundleToOperationsDispatchRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, new[] { OperationsPermissions.DispatchesManage }, cancellationToken);

        if (refused is not null)
        {
            return AddBundleToOperationsDispatchResponse.Failure(refused);
        }

        if (await _dispatches.GetDispatchAsync(request.DispatchId, cancellationToken) is not { } dispatch || warehouses.All(item => item.Id != dispatch.WarehouseId))
        {
            return AddBundleToOperationsDispatchResponse.Failure(OperationsDispatchErrors.NotFound);
        }

        OperationsBundle? bundle = request.BundleId is { } bundleId ? await _bundles.GetBundleAsync(bundleId, cancellationToken)
            : string.IsNullOrWhiteSpace(request.BundleNumber) ? null : await _dispatches.FindBundleForUpdateAsync(request.BundleNumber.Trim().ToUpperInvariant(), cancellationToken);

        if (bundle is null || bundle.Status == OperationsBundleStatus.Voided || warehouses.All(item => item.Id != bundle.HereWarehouseId))
        {
            return AddBundleToOperationsDispatchResponse.Failure(OperationsBundleErrors.NotFound);
        }

        // On it already (a double click, or scanned twice): done.
        if (bundle.DispatchId == dispatch.Id)
        {
            return AddBundleToOperationsDispatchResponse.Success(bundle.Number);
        }

        if (bundle.DispatchId is not null)
        {
            return AddBundleToOperationsDispatchResponse.Failure(OperationsDispatchErrors.BundleElsewhere);
        }

        try
        {
            dispatch.TakeBundle(bundle, _timeProvider.GetUtcNow());
            _dispatches.Add(OperationsDispatchBundle.Create(dispatch.TenantId, dispatch.Id, bundle.Id, _timeProvider.GetUtcNow()));
        }
        catch (DomainException exception)
        {
            return AddBundleToOperationsDispatchResponse.Failure(exception.Error);
        }

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return AddBundleToOperationsDispatchResponse.Success(bundle.Number);
        }

        _unitOfWork.DiscardChanges();
        return AddBundleToOperationsDispatchResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
