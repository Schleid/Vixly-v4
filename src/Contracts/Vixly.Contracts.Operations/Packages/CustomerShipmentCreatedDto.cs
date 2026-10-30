namespace Vixly.Contracts.Operations.Packages;

// =========================================================
// FILE: CustomerShipmentCreatedDto.cs
//
// PURPOSE:
// The answer to a new customer shipment (owner, 2026-10-18).
//
// RESPONSIBILITIES:
// - Carry the shipment, its first announcement and the parcel it matched, if any
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// =========================================================
public sealed record CustomerShipmentCreatedDto(Guid ShipmentId, Guid? PreAlertId, Guid? ParcelId);
