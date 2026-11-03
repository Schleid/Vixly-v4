namespace Vixly.Operations.Application.UseCases.Payables.GetOperationsShipmentProfit;

// =========================================================
// FILE: GetOperationsShipmentProfitRequest.cs
//
// PURPOSE:
// One shipment’s costs and profit, for its page (Slice 8).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsShipmentProfitRequest
{
    public Guid ShipmentId { get; init; }
}
