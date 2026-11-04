using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Printing;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Printing;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Printing.RegisterOperationsPrintAgent;

// =========================================================
// FILE: RegisterOperationsPrintAgentUseCase.cs
//
// PURPOSE:
// Registers the print program of a dock computer and gives its key once (Slice 3B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - The key is shown once; only its hash is stored
// =========================================================
public sealed class RegisterOperationsPrintAgentUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsPrintingRepository _printing;

    private readonly OperationsPrintAgentKeys _keys;

    private readonly Vixly.Operations.Application.Audit.OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public RegisterOperationsPrintAgentUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsPrintingRepository printing,
        OperationsPrintAgentKeys keys,
        Vixly.Operations.Application.Audit.OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _printing = printing ?? throw new ArgumentNullException(nameof(printing));
        _keys = keys ?? throw new ArgumentNullException(nameof(keys));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<RegisterOperationsPrintAgentResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        RegisterOperationsPrintAgentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return RegisterOperationsPrintAgentResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _scope.CoveredAsync(access, OperationsPermissions.PrintersManage, request.WarehouseId, cancellationToken) is not { } warehouse)
        {
            return RegisterOperationsPrintAgentResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        Vixly.Operations.Application.Ports.OperationsSecret secret = _keys.NewSecret();
        OperationsPrintAgent agent;

        try
        {
            agent = OperationsPrintAgent.Register(context.RequiredTenantId, warehouse.Id, request.Name, secret.Hash, _timeProvider.GetUtcNow());
        }
        catch (Vixly.Shared.Kernel.Domain.DomainException exception)
        {
            return RegisterOperationsPrintAgentResponse.Failure(exception.Error);
        }

        _printing.Add(agent);
        _audit.Record(access.User.Id, "ops.printers.agent_registered", nameof(OperationsPrintAgent), agent.Id, after: new { warehouse = warehouse.Code, name = agent.Name });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? RegisterOperationsPrintAgentResponse.Success(agent.Id, OperationsPrintAgentKeys.Format(agent.Id, secret.Value))
            : RegisterOperationsPrintAgentResponse.Failure(Vixly.Operations.Domain.Common.OperationsCommonErrors.ConcurrencyConflict);
    }
}
