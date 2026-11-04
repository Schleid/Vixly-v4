namespace Vixly.Operations.Domain.Charges;

// =========================================================
// FILE: OperationsRatePrices.cs
//
// PURPOSE:
// The prices of one version of a price list, as staff set them (Slice 4B design section 5).
//
// RESPONSIBILITIES:
// - Carry the prices into a version in one value
//
// IMPORTANT NOTES:
// - The version checks every value
// =========================================================
public sealed record OperationsRatePrices(
    IReadOnlyList<OperationsRateBand> Bands,
    IReadOnlyList<OperationsRateItem> Items,
    decimal? MinimumCharge,
    decimal? ServiceFee,
    decimal? ConsolidationFee,
    decimal? PerPackage = null,
    decimal? PerShipment = null);
