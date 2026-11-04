using Vixly.Operations.Application.Arrivals;
using Vixly.Operations.Application.Delivery;
using Vixly.Operations.Domain.Delivery;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Delivery.GetOperationsDeliveries;

// =========================================================
// FILE: GetOperationsDeliveriesResponse.cs
//
// PURPOSE:
// The delivery desk of a warehouse: shipments waiting for delivery, its routes, parcels going back (Slice 7 step 3, R-7, R-14).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsDeliveriesResponse
{
    private GetOperationsDeliveriesResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsArrivalPlace> Warehouses { get; private init; } = Array.Empty<OperationsArrivalPlace>();

    public Guid WarehouseId { get; private init; }

    public DateOnly Today { get; private init; }

    public IReadOnlyList<OperationsDeliveryWaitingRow> Waiting { get; private init; } = Array.Empty<OperationsDeliveryWaitingRow>();

    public IReadOnlyList<OperationsDeliveryRouteRow> Routes { get; private init; } = Array.Empty<OperationsDeliveryRouteRow>();

    public IReadOnlyList<OperationsDeliveryReturnRow> Returns { get; private init; } = Array.Empty<OperationsDeliveryReturnRow>();

    public IReadOnlyList<OperationsDeliveryDriver> Drivers { get; private init; } = Array.Empty<OperationsDeliveryDriver>();

    public OperationsDeliveryRecording Recording { get; private init; }

    public bool CashOnDelivery { get; private init; }

    public bool DestinationCharges { get; private init; }

    public static GetOperationsDeliveriesResponse Success(IReadOnlyList<OperationsArrivalPlace> warehouses, Guid warehouseId, DateOnly today, IReadOnlyList<OperationsDeliveryWaitingRow> waiting, IReadOnlyList<OperationsDeliveryRouteRow> routes, IReadOnlyList<OperationsDeliveryReturnRow> returns, IReadOnlyList<OperationsDeliveryDriver> drivers, OperationsDeliveryRecording recording, bool cashOnDelivery, bool destinationCharges)
    {
        return new GetOperationsDeliveriesResponse { Succeeded = true, Warehouses = warehouses, WarehouseId = warehouseId, Today = today, Waiting = waiting, Routes = routes, Returns = returns, Drivers = drivers, Recording = recording, CashOnDelivery = cashOnDelivery, DestinationCharges = destinationCharges };
    }

    public static GetOperationsDeliveriesResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsDeliveriesResponse { Error = error };
    }
}
