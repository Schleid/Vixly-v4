namespace Vixly.Operations.Application.Site;

// =========================================================
// FILE: OperationsReviewShipment.cs
//
// PURPOSE:
// The customer's last finished shipment, behind a review (owner, 2026-11-03).
//
// RESPONSIBILITIES:
// - Carry what a ticket from a review needs: the shipment, its warehouse and its number
//
// IMPORTANT NOTES:
// - Finished means delivered or picked up
// =========================================================
public sealed record OperationsReviewShipment(Guid ShipmentId, Guid WarehouseId, string Reference);
