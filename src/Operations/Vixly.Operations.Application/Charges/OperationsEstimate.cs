using Vixly.Operations.Domain.Charges;

namespace Vixly.Operations.Application.Charges;

// =========================================================
// FILE: OperationsEstimate.cs
//
// PURPOSE:
// The shipment estimate (Slice 5D): what the shipment would cost now, line by line, and what it would cost with another choice.
//
// RESPONSIBILITIES:
// - Carry the shipment's own charges, each parcel with its size and cost, the total and the currency
// - Carry the service's rates while no parcel is in (minimum, bands), so the clerk can still answer
// - Carry the "what if" choices: each insurance product, no insurance, consolidation on or off
// - Carry the supplies added at the counter (Slice 5E), in the total and in each "what if" total
//
// IMPORTANT NOTES:
// - Always an estimate (owner, 2026-10-20): no taxes, no delivery fees; the invoice comes with billing
// - Priced false: the company has no price for this service on this route
// =========================================================
public sealed record OperationsEstimate(
    bool Priced,
    string CurrencyCode,
    IReadOnlyList<OperationsChargeLine> Shipment,
    IReadOnlyList<OperationsEstimateParcel> Parcels,
    decimal Total,
    decimal? InsuredValue,
    OperationsPriceVersion? Rates,
    OperationsRateBasis Basis,
    IReadOnlyList<OperationsEstimateOption> WhatIf,
    IReadOnlyList<OperationsEstimateSupply>? Supplies = null)
{
    // Slice 5E: the supplies on the shipment, counted in the total.
    public IReadOnlyList<OperationsEstimateSupply> SupplyLines => Supplies ?? Array.Empty<OperationsEstimateSupply>();

    public static OperationsEstimate NotPriced(string currencyCode) =>
        new(false, currencyCode, Array.Empty<OperationsChargeLine>(), Array.Empty<OperationsEstimateParcel>(), 0m, null, null, OperationsRateBasis.Weight,
            Array.Empty<OperationsEstimateOption>());
}
