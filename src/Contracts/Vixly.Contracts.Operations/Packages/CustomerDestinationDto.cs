namespace Vixly.Contracts.Operations.Packages;

// =========================================================
// FILE: CustomerDestinationDto.cs
//
// PURPOSE:
// A destination warehouse the customer can ship to: Les Cayes, Jérémie (Slice 4C).
//
// RESPONSIBILITIES:
// - Carry its name, city and country, the delivery it offers and the communes it delivers to at home
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// =========================================================
public sealed record CustomerDestinationDto(Guid Id, string Name, string City, string CountryCode, bool Pickup, bool HomeDelivery, IReadOnlyList<string> HomeArea);
