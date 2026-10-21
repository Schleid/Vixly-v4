namespace Vixly.Operations.Application.Shipments;

// =========================================================
// FILE: OperationsShipmentDraft.cs
//
// PURPOSE:
// What the customer typed for a shipment's instructions (owner, 2026-10-18).
//
// RESPONSIBILITIES:
// - Carry the delivery mode, the branch or address, the recipient and the options as typed
//
// IMPORTANT NOTES:
// - Checked by OperationsShipmentIntake and the domain; never trusted as typed
// =========================================================
public sealed record OperationsShipmentDraft(
    string? Delivery,
    Guid? BranchId,
    string? Line1,
    string? Line2,
    string? City,
    string? Region,
    string? PostalCode,
    string? CountryCode,
    string? RecipientName,
    string? RecipientPhone,
    bool Consolidate,
    bool Refrigerated,
    bool Insured,
    Guid? DestinationId = null,
    bool InsuranceAccepted = false,
    string? Service = null,
    int? ParcelCount = null,
    Guid? InsurancePlanId = null);
