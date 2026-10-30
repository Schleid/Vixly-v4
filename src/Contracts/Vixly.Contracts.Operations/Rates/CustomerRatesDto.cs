namespace Vixly.Contracts.Operations.Rates;

// =========================================================
// FILE: CustomerRatesDto.cs
//
// PURPOSE:
// The customer's rate calculator: destinations, units, estimates and quotes (Slice 4 design section 8).
//
// RESPONSIBILITIES:
// - Carry everything the rates page shows
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// =========================================================
public sealed record CustomerRatesDto(
    IReadOnlyList<string> Countries,
    string WeightUnit,
    string LengthUnit,
    string Currency,
    IReadOnlyList<CustomerRateEstimateDto> Estimates,
    IReadOnlyList<CustomerQuoteDto> Quotes,
    int Unmeasured,
    string? ShipmentCountry,
    IReadOnlyList<CustomerServiceDto> Services,
    IReadOnlyList<Vixly.Contracts.Operations.Packages.CustomerDestinationDto>? Destinations = null,
    string? ShipmentDestination = null,
    IReadOnlyList<Vixly.Contracts.Operations.Packages.CustomerChoiceDto>? Origins = null);
