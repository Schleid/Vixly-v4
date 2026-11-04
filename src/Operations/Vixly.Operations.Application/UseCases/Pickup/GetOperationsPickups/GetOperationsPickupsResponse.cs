using Vixly.Operations.Application.Arrivals;
using Vixly.Operations.Application.Pickup;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Pickup.GetOperationsPickups;

// =========================================================
// FILE: GetOperationsPickupsResponse.cs
//
// PURPOSE:
// The shipments ready for pickup at a destination warehouse (Slice 7 step 2, R-9).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsPickupsResponse
{
    private GetOperationsPickupsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsArrivalPlace> Warehouses { get; private init; } = Array.Empty<OperationsArrivalPlace>();

    public Guid WarehouseId { get; private init; }

    public IReadOnlyList<OperationsPickupRow> Rows { get; private init; } = Array.Empty<OperationsPickupRow>();

    public static GetOperationsPickupsResponse Success(IReadOnlyList<OperationsArrivalPlace> warehouses, Guid warehouseId, IReadOnlyList<OperationsPickupRow> rows)
    {
        return new GetOperationsPickupsResponse { Succeeded = true, Warehouses = warehouses, WarehouseId = warehouseId, Rows = rows };
    }

    public static GetOperationsPickupsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsPickupsResponse { Error = error };
    }
}
