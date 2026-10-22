namespace Vixly.Operations.Application.Bundles;

// =========================================================
// FILE: OperationsBundleShipmentLine.cs
//
// PURPOSE:
// A shipment beside a bundle (Slice 6A, D-3): how many of its parcels are in bundles, and where the others are.
//
// RESPONSIBILITIES:
// - Carry the counts and the parcels elsewhere
//
// IMPORTANT NOTES:
// - Split: some of its parcels are in another bundle (a warning, allowed)
// =========================================================
public sealed record OperationsBundleShipmentLine(Guid ShipmentId, string? Number, int Parcels, int InBundles, int InThis, IReadOnlyList<OperationsBundleMissing> Elsewhere)
{
    public bool Split => Elsewhere.Any(parcel => parcel.BundleNumber is not null);

    public bool Complete => InBundles == Parcels;
}
