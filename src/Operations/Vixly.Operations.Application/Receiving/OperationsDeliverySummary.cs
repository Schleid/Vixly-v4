namespace Vixly.Operations.Application.Receiving;

// =========================================================
// FILE: OperationsDeliverySummary.cs
//
// PURPOSE:
// An open carrier delivery as the scan app lists it (Slice 3B).
//
// RESPONSIBILITIES:
// - Carry what the clerk needs to pick the delivery
//
// IMPORTANT NOTES:
// - Pieces counts the parcels scanned so far
// =========================================================
public sealed record OperationsDeliverySummary(Guid DeliveryId, Guid WarehouseId, string? Carrier, DateTimeOffset OpenedUtc, int Pieces);
