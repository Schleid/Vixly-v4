using Vixly.Operations.Application.Shipments;

namespace Vixly.Operations.Application.UseCases.Shipments.ListOperationsShipments;

// =========================================================
// FILE: ListOperationsShipmentsRequest.cs
//
// PURPOSE:
// The shipments desk: the shipments of the warehouses the staff member may see, by origin (Slice 5A).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListOperationsShipmentsRequest
{
    public Guid? WarehouseId { get; init; }

    public OperationsShipmentView View { get; init; } = OperationsShipmentView.ToSend;

    public string? ServiceCode { get; init; }

    public string? CountryCode { get; init; }

    public string? Search { get; init; }

    public int Page { get; init; } = 1;

    // Every line up to MaxExport, for the CSV file.
    public bool Export { get; init; }
}
