using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Supplies;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Supplies;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Supplies.ListOperationsSupplyMovements;

// =========================================================
// FILE: ListOperationsSupplyMovementsUseCase.cs
//
// PURPOSE:
// The stock movements of a supply at a warehouse, newest first (Slice 5E, S-4).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.supplies.stock, for the warehouse
// - The last 200 movements
// =========================================================
public sealed class ListOperationsSupplyMovementsUseCase
{
    private readonly OperationsSupplyDesk _desk;

    private readonly IOperationsSupplyRepository _supplies;

    private readonly IOperationsLocationRepository _locations;

    public ListOperationsSupplyMovementsUseCase(
        OperationsSupplyDesk desk,
        IOperationsSupplyRepository supplies,
        IOperationsLocationRepository locations)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _supplies = supplies ?? throw new ArgumentNullException(nameof(supplies));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
    }

    public async Task<ListOperationsSupplyMovementsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ListOperationsSupplyMovementsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? _, IReadOnlyList<OperationsWarehouse> warehouses, OperationsTenantSettings settings, Error? refused) = await _desk.OpenAsync(context, cancellationToken);

        if (refused is not null)
        {
            return ListOperationsSupplyMovementsResponse.Failure(refused);
        }

        if (warehouses.FirstOrDefault(item => item.Id == request.WarehouseId) is not { } warehouse)
        {
            return ListOperationsSupplyMovementsResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        if (await _supplies.GetAsync(request.SupplyId, cancellationToken) is not { } supply)
        {
            return ListOperationsSupplyMovementsResponse.Failure(OperationsSupplyErrors.NotFound);
        }

        return ListOperationsSupplyMovementsResponse.Success(supply, warehouse, await _supplies.GetStockAsync(supply.Id, warehouse.Id, cancellationToken),
            await _supplies.ListMovementsAsync(supply.Id, warehouse.Id, 200, cancellationToken), await _locations.ListWarehousesAsync(cancellationToken));
    }
}
