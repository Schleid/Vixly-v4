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

namespace Vixly.Operations.Application.UseCases.Dispatch.RecostOperationsDispatch;

// =========================================================
// FILE: RecostOperationsDispatchUseCase.cs
//
// PURPOSE:
// Work out a dispatch's carrier cost again from the rates in force (Slice 6B step 2, D-7): after a rate was added, or a leg's carrier changed.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.dispatches.manage at the dispatch's origin warehouse (D-8)
// =========================================================
public sealed class RecostOperationsDispatchUseCase
{
    private readonly OperationsBundleDesk _desk;

    private readonly IOperationsDispatchRepository _dispatches;

    private readonly OperationsDispatchCosting _costing;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public RecostOperationsDispatchUseCase(
        OperationsBundleDesk desk,
        IOperationsDispatchRepository dispatches,
        OperationsDispatchCosting costing,
        IOperationsSettingsRepository settings,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _dispatches = dispatches ?? throw new ArgumentNullException(nameof(dispatches));
        _costing = costing ?? throw new ArgumentNullException(nameof(costing));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<RecostOperationsDispatchResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        RecostOperationsDispatchRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, new[] { OperationsPermissions.DispatchesManage }, cancellationToken);

        if (refused is not null)
        {
            return RecostOperationsDispatchResponse.Failure(refused);
        }

        if (await _dispatches.GetDispatchAsync(request.DispatchId, cancellationToken) is not { } dispatch || warehouses.All(item => item.Id != dispatch.WarehouseId))
        {
            return RecostOperationsDispatchResponse.Failure(OperationsDispatchErrors.NotFound);
        }

        if (dispatch.Status is OperationsDispatchStatus.Planned or OperationsDispatchStatus.Cancelled)
        {
            return RecostOperationsDispatchResponse.Failure(OperationsDispatchErrors.NotClosed);
        }

        Vixly.Operations.Domain.Settings.OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken)
            ?? Vixly.Operations.Domain.Settings.OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        decimal? before = dispatch.CostComputed;
        IReadOnlyList<int> missing = await _costing.CostAsync(dispatch, settings, _timeProvider.GetUtcNow(), cancellationToken);
        _audit.Record(access!.User.Id, "ops.dispatches.costed", nameof(OperationsDispatch), dispatch.Id, before: new { computed = before }, after: new { computed = dispatch.CostComputed, total = dispatch.CostTotal });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return RecostOperationsDispatchResponse.Success(missing);
        }

        _unitOfWork.DiscardChanges();
        return RecostOperationsDispatchResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
