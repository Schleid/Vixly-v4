using Vixly.Operations.Domain.Charges;

namespace Vixly.Operations.Application.Charges;

// =========================================================
// FILE: OperationsPriceListView.cs
//
// PURPOSE:
// A price list in force, as a pricing page shows it (owner, 2026-10-21): its service, route, bands, items and fees.
//
// RESPONSIBILITIES:
// - Carry the values a pricing page shows
//
// IMPORTANT NOTES:
// - Read model only
// =========================================================
public sealed record OperationsPriceListView(string ServiceCode, string? ServiceLabel, string? Origin, string? Destination, bool Own, OperationsRateBasis Basis, OperationsTransportMode Mode,
    DateOnly EffectiveFrom, IReadOnlyList<OperationsRateBand> Bands, IReadOnlyList<OperationsPriceItemView> Items, decimal? MinimumCharge, decimal? ServiceFee, decimal? ConsolidationFee,
    decimal? PerPackage, decimal? PerShipment);
