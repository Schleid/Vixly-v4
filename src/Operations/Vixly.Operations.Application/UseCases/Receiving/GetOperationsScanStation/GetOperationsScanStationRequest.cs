namespace Vixly.Operations.Application.UseCases.Receiving.GetOperationsScanStation;

// =========================================================
// FILE: GetOperationsScanStationRequest.cs
//
// PURPOSE:
// What the scan app needs: the receiving warehouses, their printers and open deliveries (Slice 3B).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsScanStationRequest
{
    public Guid? WarehouseId { get; init; }
}
