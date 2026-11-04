namespace Vixly.Operations.Application.UseCases.Pickup.BillOperationsPickup;

// =========================================================
// FILE: BillOperationsPickupRequest.cs
//
// PURPOSE:
// Bill a shipment's destination charges and storage at the pickup counter (Slice 7 step 2, R-4, R-10).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class BillOperationsPickupRequest
{
    public Guid ShipmentId { get; init; }

    public Guid WarehouseId { get; init; }

    public decimal? Duties { get; init; }

    public decimal? LocalTax { get; init; }

    public decimal? LocalFee { get; init; }

    public Guid? RequestKey { get; init; }
}
