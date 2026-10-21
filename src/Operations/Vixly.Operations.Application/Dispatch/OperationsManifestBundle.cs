namespace Vixly.Operations.Application.Dispatch;

// =========================================================
// FILE: OperationsManifestBundle.cs
//
// PURPOSE:
// One bundle on a manifest, with its shipments (Slice 6B, D-6).
//
// RESPONSIBILITIES:
// - Carry its number, type, pieces, weight and size
//
// IMPORTANT NOTES:
// - Weights in kilograms, sizes in centimetres
// =========================================================
public sealed record OperationsManifestBundle(string Number, string? TypeName, int Pieces, decimal? WeightKg, decimal? LengthCm, decimal? WidthCm, decimal? HeightCm,
    IReadOnlyList<OperationsManifestShipment> Shipments);
