namespace Vixly.Contracts.Operations.Packages;

// =========================================================
// FILE: CustomerShipmentBody.cs
//
// PURPOSE:
// A new customer shipment: with a new package, packages announced before, or none (owner, 2026-10-18; Slice 4D).
//
// RESPONSIBILITIES:
// - Carry the origin warehouse, the instructions, the packages and the form's reference
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// =========================================================
public sealed record CustomerShipmentBody(Guid OriginWarehouseId, CustomerShipmentInstructionsBody Instructions, CustomerPreAlertBody? Package,
    IReadOnlyList<Guid>? PreAlertIds = null,
    IReadOnlyList<Guid>? ParcelIds = null,
    string? RequestKey = null);
