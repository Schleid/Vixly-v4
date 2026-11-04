using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;

namespace Vixly.Operations.Application.UseCases.Printing.ListOperationsLabelPrinters;

// =========================================================
// FILE: ListOperationsLabelPrintersUseCase.cs
//
// PURPOSE:
// Lists a warehouse's label printers for the "Print all" buttons of a shipment (owner, 2026-10-27).
//
// RESPONSIBILITIES:
// - The printers that are on, for staff who see the warehouse's parcels or serve its counter
//
// IMPORTANT NOTES:
// - Another warehouse answers like a missing one (S-3)
// =========================================================
public sealed class ListOperationsLabelPrintersUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsPrintingRepository _printing;

    public ListOperationsLabelPrintersUseCase(OperationsOwnerCheck owner, OperationsReceivingScope scope, IOperationsPrintingRepository printing)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _printing = printing ?? throw new ArgumentNullException(nameof(printing));
    }

    public async Task<ListOperationsLabelPrintersResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ListOperationsLabelPrintersRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return ListOperationsLabelPrintersResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _scope.CoveredAsync(access, OperationsPermissions.ParcelsView, request.WarehouseId, cancellationToken) is null
            && await _scope.CoveredAsync(access, OperationsPermissions.CounterServe, request.WarehouseId, cancellationToken) is null)
        {
            return ListOperationsLabelPrintersResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        return ListOperationsLabelPrintersResponse.Success(
            (await _printing.ListPrintersAsync(request.WarehouseId, cancellationToken)).Where(printer => printer.IsActive).OrderBy(printer => printer.Name, StringComparer.CurrentCulture).ToList());
    }
}
