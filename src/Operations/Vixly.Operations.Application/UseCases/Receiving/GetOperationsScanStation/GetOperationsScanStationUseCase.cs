using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.GetOperationsScanStation;

// =========================================================
// FILE: GetOperationsScanStationUseCase.cs
//
// PURPOSE:
// What the scan app needs: the receiving warehouses, their printers and open deliveries (Slice 3B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Only active receiving warehouses the person may receive at
// =========================================================
public sealed class GetOperationsScanStationUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsPrintingRepository _printing;

    public GetOperationsScanStationUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsReceivingRepository receiving,
        IOperationsPrintingRepository printing)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _printing = printing ?? throw new ArgumentNullException(nameof(printing));
    }

    public async Task<GetOperationsScanStationResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsScanStationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return GetOperationsScanStationResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        IReadOnlyList<OperationsWarehouse> warehouses = await _scope.WarehousesAsync(access, OperationsPermissions.ReceivingReceive, true, cancellationToken);

        if (warehouses.Count == 0)
        {
            return GetOperationsScanStationResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        OperationsWarehouse chosen = warehouses.FirstOrDefault(warehouse => warehouse.Id == request.WarehouseId) ?? warehouses[0];
        IReadOnlyList<Vixly.Operations.Domain.Printing.OperationsLabelPrinter> printers = (await _printing.ListPrintersAsync(chosen.Id, cancellationToken))
            .Where(printer => printer.IsActive && printer.Media == Vixly.Operations.Domain.Printing.OperationsLabelMedia.IdLabel).ToList();
        IReadOnlyList<OperationsDeliverySummary> deliveries = (await _receiving.ListOpenDeliveriesAsync(new[] { chosen.Id }, cancellationToken))
            .Select(item => new OperationsDeliverySummary(item.Delivery.Id, item.Delivery.WarehouseId, item.Delivery.Carrier, item.Delivery.CreatedUtc, item.Pieces))
            .ToList();

        return GetOperationsScanStationResponse.Success(warehouses, chosen.Id, printers, deliveries);
    }
}
