using Vixly.Operations.Application.Audit;
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

namespace Vixly.Operations.Application.UseCases.Dispatch.CorrectOperationsDispatchCost;

// =========================================================
// FILE: CorrectOperationsDispatchCostUseCase.cs
//
// PURPOSE:
// The dispatcher's cost for a dispatch, with a note (Slice 6B step 2, D-7): the carrier's invoice differs, or no rate was set.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.dispatches.manage at the dispatch's origin warehouse (D-8)
// - Audited with the total before and after
// =========================================================
public sealed class CorrectOperationsDispatchCostUseCase
{
    private readonly OperationsBundleDesk _desk;

    private readonly IOperationsDispatchRepository _dispatches;

    private readonly OperationsDispatchCosting _costing;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public CorrectOperationsDispatchCostUseCase(
        OperationsBundleDesk desk,
        IOperationsDispatchRepository dispatches,
        OperationsDispatchCosting costing,
        OperationsAuditTrail audit,
        IOperationsSettingsRepository settings,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _dispatches = dispatches ?? throw new ArgumentNullException(nameof(dispatches));
        _costing = costing ?? throw new ArgumentNullException(nameof(costing));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<CorrectOperationsDispatchCostResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        CorrectOperationsDispatchCostRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, new[] { OperationsPermissions.DispatchesManage }, cancellationToken);

        if (refused is not null)
        {
            return CorrectOperationsDispatchCostResponse.Failure(refused);
        }

        if (await _dispatches.GetDispatchAsync(request.DispatchId, cancellationToken) is not { } dispatch || warehouses.All(item => item.Id != dispatch.WarehouseId))
        {
            return CorrectOperationsDispatchCostResponse.Failure(OperationsDispatchErrors.NotFound);
        }

        object before = new { total = dispatch.CostTotal, note = dispatch.CostNote };
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        try
        {
            dispatch.CorrectCost(request.Total, request.Note, (await _settings.FindAsync(cancellationToken))?.CurrencyCode ?? "USD", utcNow);
        }
        catch (DomainException exception)
        {
            return CorrectOperationsDispatchCostResponse.Failure(exception.Error);
        }

        await _costing.ShareAsync(dispatch, utcNow, cancellationToken);
        _audit.Record(access!.User.Id, "ops.dispatches.cost_corrected", nameof(OperationsDispatch), dispatch.Id, before: before, after: new { total = dispatch.CostTotal, note = dispatch.CostNote });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return CorrectOperationsDispatchCostResponse.Success();
        }

        _unitOfWork.DiscardChanges();
        return CorrectOperationsDispatchCostResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
