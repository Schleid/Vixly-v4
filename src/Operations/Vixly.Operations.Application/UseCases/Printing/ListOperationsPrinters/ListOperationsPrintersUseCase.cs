using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Printing;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Printing;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Printing.ListOperationsPrinters;

// =========================================================
// FILE: ListOperationsPrintersUseCase.cs
//
// PURPOSE:
// The label printers and dock print programs of a warehouse (Slice 3B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.printers.manage for that warehouse
// =========================================================
public sealed class ListOperationsPrintersUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsPrintingRepository _printing;

    public ListOperationsPrintersUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsPrintingRepository printing)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _printing = printing ?? throw new ArgumentNullException(nameof(printing));
    }

    public async Task<ListOperationsPrintersResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ListOperationsPrintersRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return ListOperationsPrintersResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _scope.CoveredAsync(access, OperationsPermissions.PrintersManage, request.WarehouseId, cancellationToken) is not { } warehouse)
        {
            return ListOperationsPrintersResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        return ListOperationsPrintersResponse.Success(warehouse, await _printing.ListPrintersAsync(warehouse.Id, cancellationToken), await _printing.ListAgentsAsync(warehouse.Id, cancellationToken));
    }
}
