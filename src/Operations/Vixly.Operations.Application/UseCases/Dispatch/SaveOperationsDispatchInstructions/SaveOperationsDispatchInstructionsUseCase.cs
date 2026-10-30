using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Application.Dispatch;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Dispatch.SaveOperationsDispatchInstructions;

// =========================================================
// FILE: SaveOperationsDispatchInstructionsUseCase.cs
//
// PURPOSE:
// What the Shipper's Letter of Instruction adds to the dispatch (Slice 6B step 2, D-6): charges, export reference, documents, handling.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.dispatches.manage at the dispatch's origin warehouse (D-8)
// =========================================================
public sealed class SaveOperationsDispatchInstructionsUseCase
{
    private readonly OperationsBundleDesk _desk;

    private readonly IOperationsDispatchRepository _dispatches;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SaveOperationsDispatchInstructionsUseCase(
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

    public async Task<SaveOperationsDispatchInstructionsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SaveOperationsDispatchInstructionsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, new[] { OperationsPermissions.DispatchesManage }, cancellationToken);

        if (refused is not null)
        {
            return SaveOperationsDispatchInstructionsResponse.Failure(refused);
        }

        if (await _dispatches.GetDispatchAsync(request.DispatchId, cancellationToken) is not { } dispatch || warehouses.All(item => item.Id != dispatch.WarehouseId))
        {
            return SaveOperationsDispatchInstructionsResponse.Failure(OperationsDispatchErrors.NotFound);
        }

        try
        {
            dispatch.ChangeInstructions(request.ChargesCollect, request.ExportReference, request.Documents, request.HandlingInstructions, _timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return SaveOperationsDispatchInstructionsResponse.Failure(exception.Error);
        }

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return SaveOperationsDispatchInstructionsResponse.Success();
        }

        _unitOfWork.DiscardChanges();
        return SaveOperationsDispatchInstructionsResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
