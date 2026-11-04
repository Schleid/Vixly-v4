using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Supplies;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Supplies;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Supplies.ListOperationsLowSupplies;

// =========================================================
// FILE: ListOperationsLowSuppliesUseCase.cs
//
// PURPOSE:
// The supplies at or below their reorder level in the warehouses a person keeps the stock of (Slice 5E, S-8), for the staff home page.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.supplies.stock, per warehouse; nothing for anyone else
// - At most 50 lines
// =========================================================
public sealed class ListOperationsLowSuppliesUseCase
{
    private readonly OperationsSupplyDesk _desk;

    private readonly IOperationsSupplyRepository _supplies;

    public ListOperationsLowSuppliesUseCase(
        OperationsSupplyDesk desk,
        IOperationsSupplyRepository supplies)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _supplies = supplies ?? throw new ArgumentNullException(nameof(supplies));
    }

    public async Task<ListOperationsLowSuppliesResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ListOperationsLowSuppliesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? _, IReadOnlyList<OperationsWarehouse> warehouses, OperationsTenantSettings settings, Error? refused) = await _desk.OpenAsync(context, cancellationToken);

        // Quiet: a person who keeps no stock, or a company that does not count it, sees nothing.
        if (refused is not null || !settings.CountSupplyStock)
        {
            return ListOperationsLowSuppliesResponse.Success(Array.Empty<OperationsLowSupply>());
        }

        Dictionary<Guid, OperationsWarehouse> kept = warehouses.ToDictionary(warehouse => warehouse.Id);
        Dictionary<Guid, OperationsSupply> catalogue = (await _supplies.ListAsync(cancellationToken)).Where(supply => supply.IsActive).ToDictionary(supply => supply.Id);
        List<OperationsLowSupply> items = (await _supplies.ListStockAsync(null, cancellationToken))
            .Where(stock => stock.IsLow && kept.ContainsKey(stock.WarehouseId) && catalogue.ContainsKey(stock.SupplyId))
            .Select(stock => new OperationsLowSupply(stock.WarehouseId, kept[stock.WarehouseId].Code, stock.SupplyId, catalogue[stock.SupplyId].Code,
                catalogue[stock.SupplyId].Name(request.Language), stock.OnHand, stock.ReorderLevel!.Value))
            .OrderBy(item => item.WarehouseCode, StringComparer.Ordinal).ThenBy(item => item.SupplyCode, StringComparer.Ordinal).Take(50).ToList();
        return ListOperationsLowSuppliesResponse.Success(items);
    }
}
