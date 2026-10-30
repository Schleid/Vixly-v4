namespace Vixly.Contracts.Operations.Packages;

// =========================================================
// FILE: CustomerShipmentInstructionsBody.cs
//
// PURPOSE:
// Where a customer shipment goes, who receives it and its options (owner, 2026-10-18).
//
// RESPONSIBILITIES:
// - Carry the delivery mode (pickup or home), the branch or address, the recipient and the options
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// =========================================================
public sealed record CustomerShipmentInstructionsBody(string? Delivery, Guid? BranchId, CustomerAddressDto? Address, string? RecipientName, string? RecipientPhone, bool Consolidate, bool Refrigerated, bool Insured,
    Guid? DestinationId = null,
    bool InsuranceAccepted = false,
    string? Service = null,
    int? ParcelCount = null,
    Guid? InsurancePlanId = null,
    bool Collect = false);
