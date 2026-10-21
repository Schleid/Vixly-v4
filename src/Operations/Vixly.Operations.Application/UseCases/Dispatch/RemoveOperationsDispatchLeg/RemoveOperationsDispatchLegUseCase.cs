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

namespace Vixly.Operations.Application.UseCases.Dispatch.RemoveOperationsDispatchLeg;

// =========================================================
// FILE: RemoveOperationsDispatchLegUseCase.cs
//
// PURPOSE:
// Remove a leg of a dispatch before it leaves (Slice 6B, D-4).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.dispatches.manage at the dispatch's origin warehouse (D-8)
// =========================================================
public sealed class RemoveOperationsDispatchLegUseCase
{
    private readonly OperationsBundleDesk _desk;

    private readonly IOperationsDispatchRepository _dispatches;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public RemoveOperationsDispatchLegUseCase(
        OperationsBundleDesk desk,
        IOperationsDispatchRepository dispatches,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _dispatches = dispatches ?? throw new ArgumentNullException(nameof(dispatches));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<RemoveOperationsDispatchLegResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        RemoveOperationsDispatchLegRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, new[] { OperationsPermissions.DispatchesManage }, cancellationToken);

        if (refused is not null)
        {
            return RemoveOperationsDispatchLegResponse.Failure(refused);
        }

        if (await _dispatches.GetDispatchAsync(request.DispatchId, cancellationToken) is not { } dispatch || warehouses.All(item => item.Id != dispatch.WarehouseId))
        {
            return RemoveOperationsDispatchLegResponse.Failure(OperationsDispatchErrors.NotFound);
        }

        // Removed already (a double click): done.
        if (dispatch.Legs.All(item => item.Id != request.LegId))
        {
            return RemoveOperationsDispatchLegResponse.Success();
        }

        try
        {
            dispatch.RemoveLeg(request.LegId, _timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return RemoveOperationsDispatchLegResponse.Failure(exception.Error);
        }

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return RemoveOperationsDispatchLegResponse.Success();
        }

        _unitOfWork.DiscardChanges();
        return RemoveOperationsDispatchLegResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
