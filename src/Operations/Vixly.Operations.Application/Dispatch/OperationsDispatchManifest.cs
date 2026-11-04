using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Application.Dispatch;

// =========================================================
// FILE: OperationsDispatchManifest.cs
//
// PURPOSE:
// What a dispatch manifest shows (Slice 6B, D-6): for the carrier and customs.
//
// RESPONSIBILITIES:
// - Carry the company, the dispatch, its legs and its bundles with their shipments
//
// IMPORTANT NOTES:
// - A manifest of a planned dispatch is marked draft
// =========================================================
public sealed record OperationsDispatchManifest(string CompanyName, string Number, string OriginCode, string DestinationCode, string ServiceCode, bool Draft, DateTimeOffset? CutoffUtc,
    IReadOnlyList<OperationsManifestLeg> Legs, IReadOnlyList<OperationsManifestBundle> Bundles, OperationsWeightUnit WeightUnit, OperationsDimensionUnit DimensionUnit, string? TimeZone);
