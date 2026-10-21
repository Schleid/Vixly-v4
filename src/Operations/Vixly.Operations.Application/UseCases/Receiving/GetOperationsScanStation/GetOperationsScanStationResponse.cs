using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.GetOperationsScanStation;

// =========================================================
// FILE: GetOperationsScanStationResponse.cs
//
// PURPOSE:
// What the scan app needs: the receiving warehouses, their printers and open deliveries (Slice 3B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsScanStationResponse
{
    private GetOperationsScanStationResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsWarehouse> Warehouses { get; private init; } = Array.Empty<OperationsWarehouse>();

    public Guid? WarehouseId { get; private init; }

    public IReadOnlyList<Vixly.Operations.Domain.Printing.OperationsLabelPrinter> Printers { get; private init; } = Array.Empty<Vixly.Operations.Domain.Printing.OperationsLabelPrinter>();

    public IReadOnlyList<OperationsDeliverySummary> Deliveries { get; private init; } = Array.Empty<OperationsDeliverySummary>();

    public static GetOperationsScanStationResponse Success(IReadOnlyList<OperationsWarehouse> warehouses, Guid? warehouseId, IReadOnlyList<Vixly.Operations.Domain.Printing.OperationsLabelPrinter> printers, IReadOnlyList<OperationsDeliverySummary> deliveries)
    {
        return new GetOperationsScanStationResponse { Succeeded = true, Warehouses = warehouses, WarehouseId = warehouseId, Printers = printers, Deliveries = deliveries };
    }

    public static GetOperationsScanStationResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsScanStationResponse { Error = error };
    }
}
