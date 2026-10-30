namespace Vixly.Contracts.Operations.Packages;

// =========================================================
// FILE: CustomerShipmentChoicesDto.cs
//
// PURPOSE:
// What a customer may choose when creating a shipment (owner, 2026-10-18).
//
// RESPONSIBILITIES:
// - Carry the origins, pickup branches, delivery modes offered and the default recipient
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// =========================================================
public sealed record CustomerShipmentChoicesDto(IReadOnlyList<CustomerChoiceDto> Origins, IReadOnlyList<CustomerChoiceDto> Branches, bool Pickup, bool HomeDelivery, string RecipientName, string RecipientPhone, IReadOnlyList<string>? Countries = null,
    IReadOnlyList<CustomerDestinationDto>? Destinations = null,
    IReadOnlyList<CustomerInsuranceTermsDto>? Insurance = null,
    IReadOnlyList<string>? Services = null,
    IReadOnlyList<CustomerLiabilityDto>? Liability = null,
    bool AllowCollect = false);
