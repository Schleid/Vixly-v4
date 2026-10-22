namespace Vixly.Contracts.Operations.Rates;

// =========================================================
// FILE: CustomerShipmentEstimateDto.cs
//
// PURPOSE:
// The shipment estimate for the portal (Slice 5D): the shipment's charges, each parcel, the total, the rates while no parcel is in, and the what if choices.
//
// RESPONSIBILITIES:
// - Carry the values between Operations and the customer portal
//
// IMPORTANT NOTES:
// - Codes, numbers and the customer's own names only: the portal writes the words (ADR-0014)
// =========================================================
public sealed record CustomerShipmentEstimateDto(
    bool Priced,
    string Currency,
    string WeightUnit,
    string DimensionUnit,
    string Basis,
    IReadOnlyList<CustomerChargeLineDto> Shipment,
    IReadOnlyList<CustomerEstimateParcelDto> Parcels,
    decimal Total,
    decimal? MinimumCharge,
    IReadOnlyList<CustomerRateBandDto> Bands,
    IReadOnlyList<CustomerEstimateOptionDto> WhatIf,
    IReadOnlyList<CustomerEstimateSupplyDto>? Supplies = null);
