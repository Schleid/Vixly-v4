namespace Vixly.Contracts.Operations.Packages;

// =========================================================
// FILE: CustomerShipmentDetailDto.cs
//
// PURPOSE:
// A customer shipment with its packages (owner, 2026-10-18).
//
// RESPONSIBILITIES:
// - Carry the shipment, its open announcements, its arrived parcels and whether it may be cancelled
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// =========================================================
public sealed record CustomerShipmentDetailDto(CustomerShipmentDto Shipment, IReadOnlyList<CustomerPreAlertDto> PreAlerts, IReadOnlyList<CustomerParcelDto> Parcels, bool CanCancel, string WeightUnit, string DimensionUnit);
