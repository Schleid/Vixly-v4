using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Supplies;

namespace Vixly.Operations.Application.Supplies;

// =========================================================
// FILE: OperationsSupplyStockKeeper.cs
//
// PURPOSE:
// Applies a stock movement (Slice 5E, S-4): the movement is recorded and the warehouse's balance follows it.
//
// RESPONSIBILITIES:
// - Find or start the stock row of the supply at the warehouse
// - Record the movement and move the balance
//
// IMPORTANT NOTES:
// - The only way the balance changes; saved by the caller's unit of work
// =========================================================
public sealed class OperationsSupplyStockKeeper
{
    private readonly IOperationsSupplyRepository _supplies;

    public OperationsSupplyStockKeeper(IOperationsSupplyRepository supplies)
    {
        _supplies = supplies ?? throw new ArgumentNullException(nameof(supplies));
    }

    public async Task<OperationsSupplyStock> MoveAsync(OperationsSupplyMovement movement, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(movement);
        OperationsSupplyStock? stock = await _supplies.GetStockAsync(movement.SupplyId, movement.WarehouseId, cancellationToken);

        if (stock is null)
        {
            stock = OperationsSupplyStock.Create(movement.TenantId, movement.SupplyId, movement.WarehouseId, utcNow);
            _supplies.Add(stock);
        }

        stock.Apply(movement, utcNow);
        _supplies.Add(movement);
        return stock;
    }
}
