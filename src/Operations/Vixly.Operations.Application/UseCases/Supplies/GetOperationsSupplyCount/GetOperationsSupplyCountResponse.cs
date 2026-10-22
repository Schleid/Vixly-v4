using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Supplies;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Supplies.GetOperationsSupplyCount;

// =========================================================
// FILE: GetOperationsSupplyCountResponse.cs
//
// PURPOSE:
// A stock count with the supplies to count and their stock on hand (Slice 5E, S-7).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsSupplyCountResponse
{
    private GetOperationsSupplyCountResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsSupplyCount Count { get; private init; } = null!;

    public OperationsWarehouse Warehouse { get; private init; } = null!;

    public IReadOnlyList<OperationsSupply> Supplies { get; private init; } = Array.Empty<OperationsSupply>();

    public IReadOnlyList<OperationsSupplyStock> Stock { get; private init; } = Array.Empty<OperationsSupplyStock>();

    public static GetOperationsSupplyCountResponse Success(OperationsSupplyCount count, OperationsWarehouse warehouse, IReadOnlyList<OperationsSupply> supplies, IReadOnlyList<OperationsSupplyStock> stock)
    {
        return new GetOperationsSupplyCountResponse { Succeeded = true, Count = count, Warehouse = warehouse, Supplies = supplies, Stock = stock };
    }

    public static GetOperationsSupplyCountResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsSupplyCountResponse { Error = error };
    }
}
