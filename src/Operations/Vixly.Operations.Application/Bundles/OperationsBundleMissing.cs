namespace Vixly.Operations.Application.Bundles;

// =========================================================
// FILE: OperationsBundleMissing.cs
//
// PURPOSE:
// A parcel of a shipment that is not in the bundle being looked at (Slice 6A, D-3): where it is.
//
// RESPONSIBILITIES:
// - Carry its number, its shelf and the bundle it is in, if any
//
// IMPORTANT NOTES:
// - None for a parcel in no bundle and on no shelf
// =========================================================
public sealed record OperationsBundleMissing(Guid ParcelId, string? Number, string? ShelfCode, string? BundleNumber);
