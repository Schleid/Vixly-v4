namespace Vixly.Operations.Application.Bundles;

// =========================================================
// FILE: OperationsBundleParcelRow.cs
//
// PURPOSE:
// A parcel in a bundle (Slice 6A, D-10).
//
// RESPONSIBILITIES:
// - Carry what the bundle page shows of it
//
// IMPORTANT NOTES:
// - Its box when it went in with a repacked shipment
// =========================================================
public sealed record OperationsBundleParcelRow(Guid ParcelId, string? Number, Guid? ShipmentId, string? ShipmentNumber, decimal? WeightKg, Guid? BoxId, string? BoxNumber);
