using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Supplies;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Supplies.GetOperationsSupplyStock;

// =========================================================
// FILE: GetOperationsSupplyStockResponse.cs
//
// PURPOSE:
// The supplies stock of a warehouse (Slice 5E, S-4 to S-8): on hand, reorder levels, low stock, transfers in transit and the open count.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsSupplyStockResponse
{
    private GetOperationsSupplyStockResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsWarehouse> Warehouses { get; private init; } = Array.Empty<OperationsWarehouse>();

    public OperationsWarehouse? Warehouse { get; private init; }

    public IReadOnlyList<OperationsWarehouse> AllWarehouses { get; private init; } = Array.Empty<OperationsWarehouse>();

    public IReadOnlyList<OperationsSupply> Supplies { get; private init; } = Array.Empty<OperationsSupply>();

    public IReadOnlyList<OperationsSupplyStock> Stock { get; private init; } = Array.Empty<OperationsSupplyStock>();

    public IReadOnlyList<OperationsSupplyTransfer> Transfers { get; private init; } = Array.Empty<OperationsSupplyTransfer>();

    public OperationsSupplyCount? OpenCount { get; private init; }

    public bool CountsStock { get; private init; }

    public OperationsSupplyStockOut StockOut { get; private init; }

    public static GetOperationsSupplyStockResponse Success(IReadOnlyList<OperationsWarehouse> warehouses, OperationsWarehouse? warehouse, IReadOnlyList<OperationsWarehouse> allWarehouses, IReadOnlyList<OperationsSupply> supplies, IReadOnlyList<OperationsSupplyStock> stock, IReadOnlyList<OperationsSupplyTransfer> transfers, OperationsSupplyCount? openCount, bool countsStock, OperationsSupplyStockOut stockOut)
    {
        return new GetOperationsSupplyStockResponse { Succeeded = true, Warehouses = warehouses, Warehouse = warehouse, AllWarehouses = allWarehouses, Supplies = supplies, Stock = stock, Transfers = transfers, OpenCount = openCount, CountsStock = countsStock, StockOut = stockOut };
    }

    public static GetOperationsSupplyStockResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsSupplyStockResponse { Error = error };
    }
}
