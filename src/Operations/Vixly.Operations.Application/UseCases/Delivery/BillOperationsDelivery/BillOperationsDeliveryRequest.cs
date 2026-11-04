namespace Vixly.Operations.Application.UseCases.Delivery.BillOperationsDelivery;

// =========================================================
// FILE: BillOperationsDeliveryRequest.cs
//
// PURPOSE:
// Bill a shipment's destination charges before it goes out for delivery (Slice 7 step 3, R-4, R-9, R-13).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class BillOperationsDeliveryRequest
{
    public Guid WarehouseId { get; init; }

    public Guid ShipmentId { get; init; }

    public decimal? Duties { get; init; }

    public decimal? LocalTax { get; init; }

    public decimal? LocalFee { get; init; }

    public Guid? RequestKey { get; init; }
}
