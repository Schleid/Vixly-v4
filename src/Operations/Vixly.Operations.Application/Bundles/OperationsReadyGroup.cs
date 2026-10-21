namespace Vixly.Operations.Application.Bundles;

// =========================================================
// FILE: OperationsReadyGroup.cs
//
// PURPOSE:
// Parcels ready to bundle for one destination and one service at a warehouse (Slice 6A, D-10).
//
// RESPONSIBILITIES:
// - Carry the counts and the weight the ship manager needs to open bundles
//
// IMPORTANT NOTES:
// - Waiting: parcels of final shipments still unpaid or held; not counted as ready
// =========================================================
public sealed record OperationsReadyGroup(Guid WarehouseId, Guid? DestinationWarehouseId, string CountryCode, string ServiceCode, int Parcels, int Shipments, decimal WeightKg, decimal VolumeCm3,
    int Waiting, int Consolidated);
