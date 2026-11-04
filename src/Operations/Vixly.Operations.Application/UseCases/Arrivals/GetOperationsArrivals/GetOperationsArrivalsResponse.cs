using Vixly.Operations.Application.Arrivals;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Arrivals.GetOperationsArrivals;

// =========================================================
// FILE: GetOperationsArrivalsResponse.cs
//
// PURPOSE:
// The dispatches a destination warehouse receives (Slice 7, R-1).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsArrivalsResponse
{
    private GetOperationsArrivalsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsArrivalPlace> Warehouses { get; private init; } = Array.Empty<OperationsArrivalPlace>();

    public Guid WarehouseId { get; private init; }

    public IReadOnlyList<OperationsArrivalRow> Coming { get; private init; } = Array.Empty<OperationsArrivalRow>();

    public IReadOnlyList<OperationsArrivalRow> Received { get; private init; } = Array.Empty<OperationsArrivalRow>();

    public static GetOperationsArrivalsResponse Success(IReadOnlyList<OperationsArrivalPlace> warehouses, Guid warehouseId, IReadOnlyList<OperationsArrivalRow> coming, IReadOnlyList<OperationsArrivalRow> received)
    {
        return new GetOperationsArrivalsResponse { Succeeded = true, Warehouses = warehouses, WarehouseId = warehouseId, Coming = coming, Received = received };
    }

    public static GetOperationsArrivalsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsArrivalsResponse { Error = error };
    }
}
