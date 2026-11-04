using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Supplies;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Supplies;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Supplies.GetOperationsSupplyStock;

// =========================================================
// FILE: GetOperationsSupplyStockUseCase.cs
//
// PURPOSE:
// The supplies stock of a warehouse (Slice 5E, S-4 to S-8): on hand, reorder levels, low stock, transfers in transit and the open count.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.supplies.stock, per warehouse; a warehouse the person does not keep shows the first one they keep
// =========================================================
public sealed class GetOperationsSupplyStockUseCase
{
    private readonly OperationsSupplyDesk _desk;

    private readonly IOperationsSupplyRepository _supplies;

    private readonly IOperationsLocationRepository _locations;

    public GetOperationsSupplyStockUseCase(
        OperationsSupplyDesk desk,
        IOperationsSupplyRepository supplies,
        IOperationsLocationRepository locations)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _supplies = supplies ?? throw new ArgumentNullException(nameof(supplies));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
    }

    public async Task<GetOperationsSupplyStockResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsSupplyStockRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? _, IReadOnlyList<OperationsWarehouse> warehouses, OperationsTenantSettings settings, Error? refused) = await _desk.OpenAsync(context, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsSupplyStockResponse.Failure(refused);
        }

        OperationsWarehouse warehouse = warehouses.FirstOrDefault(item => item.Id == request.WarehouseId) ?? warehouses[0];
        return GetOperationsSupplyStockResponse.Success(
            warehouses,
            warehouse,
            await _locations.ListWarehousesAsync(cancellationToken),
            await _supplies.ListAsync(cancellationToken),
            await _supplies.ListStockAsync(warehouse.Id, cancellationToken),
            await _supplies.ListTransfersInTransitAsync(warehouse.Id, cancellationToken),
            await _supplies.FindOpenCountAsync(warehouse.Id, cancellationToken),
            settings.CountSupplyStock,
            settings.SupplyStockOut);
    }
}
