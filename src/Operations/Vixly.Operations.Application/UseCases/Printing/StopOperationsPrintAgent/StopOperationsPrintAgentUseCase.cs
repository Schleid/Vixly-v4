using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Printing;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Printing;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Printing.StopOperationsPrintAgent;

// =========================================================
// FILE: StopOperationsPrintAgentUseCase.cs
//
// PURPOSE:
// Stops a dock print program: its key no longer collects labels (Slice 3B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.printers.manage for that warehouse
// =========================================================
public sealed class StopOperationsPrintAgentUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsPrintingRepository _printing;

    private readonly Vixly.Operations.Application.Audit.OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public StopOperationsPrintAgentUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsPrintingRepository printing,
        Vixly.Operations.Application.Audit.OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _printing = printing ?? throw new ArgumentNullException(nameof(printing));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<StopOperationsPrintAgentResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        StopOperationsPrintAgentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return StopOperationsPrintAgentResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _scope.CoveredAsync(access, OperationsPermissions.PrintersManage, request.WarehouseId, cancellationToken) is not { } warehouse)
        {
            return StopOperationsPrintAgentResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        if (await _printing.GetAgentAsync(request.AgentId, cancellationToken) is not { } agent || agent.WarehouseId != warehouse.Id)
        {
            return StopOperationsPrintAgentResponse.Failure(OperationsPrintingErrors.AgentNotFound);
        }

        agent.Stop(_timeProvider.GetUtcNow());
        _audit.Record(access.User.Id, "ops.printers.agent_stopped", nameof(OperationsPrintAgent), agent.Id, after: new { warehouse = warehouse.Code, name = agent.Name });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? StopOperationsPrintAgentResponse.Success(agent.Id)
            : StopOperationsPrintAgentResponse.Failure(Vixly.Operations.Domain.Common.OperationsCommonErrors.ConcurrencyConflict);
    }
}
