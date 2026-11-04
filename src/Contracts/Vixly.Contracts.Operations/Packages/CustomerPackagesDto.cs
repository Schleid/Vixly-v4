namespace Vixly.Contracts.Operations.Packages;

// =========================================================
// FILE: CustomerPackagesDto.cs
//
// PURPOSE:
// The customer's open pre-alerts and packages (Slice 3B).
//
// RESPONSIBILITIES:
// - Carry the values between Customer.Web and Operations.Api
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// =========================================================
public sealed record CustomerPackagesDto(IReadOnlyList<CustomerPreAlertDto> PreAlerts, IReadOnlyList<CustomerParcelDto> Parcels, string WeightUnit, string DimensionUnit,
    IReadOnlyList<CustomerShipmentDto>? Shipments = null);
