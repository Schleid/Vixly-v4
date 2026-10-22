using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Supplies;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Supplies.ListOperationsSupplyMovements;

// =========================================================
// FILE: ListOperationsSupplyMovementsResponse.cs
//
// PURPOSE:
// The stock movements of a supply at a warehouse, newest first (Slice 5E, S-4).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListOperationsSupplyMovementsResponse
{
    private ListOperationsSupplyMovementsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsSupply Supply { get; private init; } = null!;

    public OperationsWarehouse Warehouse { get; private init; } = null!;

    public OperationsSupplyStock? Stock { get; private init; }

    public IReadOnlyList<OperationsSupplyMovement> Movements { get; private init; } = Array.Empty<OperationsSupplyMovement>();

    public IReadOnlyList<OperationsWarehouse> AllWarehouses { get; private init; } = Array.Empty<OperationsWarehouse>();

    public static ListOperationsSupplyMovementsResponse Success(OperationsSupply supply, OperationsWarehouse warehouse, OperationsSupplyStock? stock, IReadOnlyList<OperationsSupplyMovement> movements, IReadOnlyList<OperationsWarehouse> allWarehouses)
    {
        return new ListOperationsSupplyMovementsResponse { Succeeded = true, Supply = supply, Warehouse = warehouse, Stock = stock, Movements = movements, AllWarehouses = allWarehouses };
    }

    public static ListOperationsSupplyMovementsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListOperationsSupplyMovementsResponse { Error = error };
    }
}
