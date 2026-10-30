namespace Vixly.Operations.Application.Bundles;

// =========================================================
// FILE: OperationsBundleCandidate.cs
//
// PURPOSE:
// A parcel or a closed repack box that may be put on a bundle's list from the computer (owner, 2026-10-21).
//
// RESPONSIBILITIES:
// - Carry its number, its shipment and how many parcels it holds
//
// IMPORTANT NOTES:
// - Read model only
// =========================================================
public sealed record OperationsBundleCandidate(Guid Id, bool IsBox, string Number, Guid ShipmentId, string? ShipmentNumber, int Parcels, decimal WeightKg);
