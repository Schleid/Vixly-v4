using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Application.Dispatch;

// =========================================================
// FILE: OperationsBundleManifest.cs
//
// PURPOSE:
// What a bundle manifest shows (Slice 6B, D-6): what is inside one bundle.
//
// RESPONSIBILITIES:
// - Carry the company, the bundle, its route and its shipments
//
// IMPORTANT NOTES:
// - Its dispatch number once it is on one
// =========================================================
public sealed record OperationsBundleManifest(string CompanyName, string OriginCode, string DestinationCode, string ServiceCode, string? DispatchNumber, OperationsManifestBundle Bundle,
    OperationsWeightUnit WeightUnit, OperationsDimensionUnit DimensionUnit);
