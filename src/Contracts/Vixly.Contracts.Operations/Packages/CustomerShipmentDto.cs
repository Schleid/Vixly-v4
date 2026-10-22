namespace Vixly.Contracts.Operations.Packages;

// =========================================================
// FILE: CustomerShipmentDto.cs
//
// PURPOSE:
// A customer shipment as its customer sees it (owner, 2026-10-18).
//
// RESPONSIBILITIES:
// - Carry the instructions, the origin and destination names and the counts of announced and arrived packages
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// =========================================================
public sealed record CustomerShipmentDto(Guid ShipmentId, bool HasInstructions, bool IsOpen, string? Origin, string? Delivery, Guid? BranchId, string? BranchName, CustomerAddressDto? Address, string? CountryCode, string? RecipientName, string? RecipientPhone, bool Consolidate, bool Refrigerated, bool Insured, int Announced, int Arrived, DateTimeOffset CreatedUtc,
    Guid? DestinationId = null,
    string? Service = null,
    Guid? OriginId = null,
    int? ParcelCount = null,
    bool Booked = false,
    decimal? BookedTotal = null,
    string? BookedCurrency = null,
    Guid? InsurancePlanId = null,
    string? CollectorName = null,
    string? CollectorPhone = null,
    DateTimeOffset? PickedUpUtc = null,
    DateTimeOffset? DeliveredUtc = null,
    bool Collect = false);
