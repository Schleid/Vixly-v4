using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Application.Charges;

// =========================================================
// FILE: OperationsPricingView.cs
//
// PURPOSE:
// The company's prices in force for a pricing page (owner, 2026-10-21), with its units and currency.
//
// RESPONSIBILITIES:
// - Carry the values a pricing page shows
//
// IMPORTANT NOTES:
// - Read model only
// =========================================================
public sealed record OperationsPricingView(IReadOnlyList<OperationsPriceListView> Lists, OperationsWeightUnit WeightUnit, OperationsDimensionUnit DimensionUnit, string CurrencyCode);
