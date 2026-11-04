using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Shipments;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Shipments.ListOperationsShipments;

// =========================================================
// FILE: ListOperationsShipmentsResponse.cs
//
// PURPOSE:
// The shipments desk: the shipments of the warehouses the staff member may see, by origin (Slice 5A).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListOperationsShipmentsResponse
{
    private ListOperationsShipmentsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    // Slice 5F (C-7): where each final shipment's invoice stands (none: not final or made final before billing).
    public IReadOnlyDictionary<Guid, Vixly.Operations.Domain.Billing.OperationsPaymentState> Payments { get; private set; } =
        new Dictionary<Guid, Vixly.Operations.Domain.Billing.OperationsPaymentState>();

    internal ListOperationsShipmentsResponse WithPayments(IReadOnlyDictionary<Guid, Vixly.Operations.Domain.Billing.OperationsPaymentState> payments)
    {
        Payments = payments;
        return this;
    }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsShipmentRow> Rows { get; private init; } = Array.Empty<OperationsShipmentRow>();

    public int TotalCount { get; private init; }

    public OperationsShipmentCounts Counts { get; private init; } = new(0, 0, 0);

    public IReadOnlyList<OperationsWarehouse> Warehouses { get; private init; } = Array.Empty<OperationsWarehouse>();

    public static ListOperationsShipmentsResponse Success(IReadOnlyList<OperationsShipmentRow> rows, int totalCount, OperationsShipmentCounts counts, IReadOnlyList<OperationsWarehouse> warehouses)
    {
        return new ListOperationsShipmentsResponse { Succeeded = true, Rows = rows, TotalCount = totalCount, Counts = counts, Warehouses = warehouses };
    }

    public static ListOperationsShipmentsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListOperationsShipmentsResponse { Error = error };
    }
}
