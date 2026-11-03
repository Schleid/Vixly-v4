using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Printing;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Printing;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.UseCases.Printing.CreateOperationsPrintPairing;

// =========================================================
// FILE: CreateOperationsPrintPairingUseCase.cs
//
// PURPOSE:
// Makes the one-time code a phone running Vixly Print scans to join a warehouse (Slice 12E, owner H-6).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs Manage printers at the warehouse
// - The code works once, for 10 minutes; only its hash is kept
// =========================================================
public sealed class CreateOperationsPrintPairingUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly Vixly.Operations.Application.Receiving.OperationsReceivingScope _scope;

    private readonly IOperationsPrintingRepository _printing;

    private readonly IOperationsSecretGenerator _secrets;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public CreateOperationsPrintPairingUseCase(
        OperationsOwnerCheck owner,
        Vixly.Operations.Application.Receiving.OperationsReceivingScope scope,
        IOperationsPrintingRepository printing,
        IOperationsSecretGenerator secrets,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _printing = printing ?? throw new ArgumentNullException(nameof(printing));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<CreateOperationsPrintPairingResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        CreateOperationsPrintPairingRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return CreateOperationsPrintPairingResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _scope.CoveredAsync(access, OperationsPermissions.PrintersManage, request.WarehouseId, cancellationToken) is not { } warehouse)
        {
            return CreateOperationsPrintPairingResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        string code = OperationsPrintPairingCode.New();
        OperationsPrintPairing pairing;

        try
        {
            pairing = OperationsPrintPairing.Create(context.RequiredTenantId, warehouse.Id, _secrets.HashToken(code), request.Name, access.User.Id, _timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return CreateOperationsPrintPairingResponse.Failure(exception.Error);
        }

        _printing.Add(pairing);
        _audit.Record(access.User.Id, "ops.printers.phone_pairing", nameof(OperationsPrintPairing), pairing.Id, after: new { warehouse = warehouse.Code, name = pairing.Name });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? CreateOperationsPrintPairingResponse.Success(code, pairing.ExpiresUtc)
            : CreateOperationsPrintPairingResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
