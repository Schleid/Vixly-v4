namespace Vixly.Contracts.Operations.Packages;

// =========================================================
// FILE: CustomerAddressDto.cs
//
// PURPOSE:
// A delivery address on a customer shipment (owner, 2026-10-18).
//
// RESPONSIBILITIES:
// - Carry the lines, city, region, postal code and country
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// =========================================================
public sealed record CustomerAddressDto(string? Line1, string? Line2, string? City, string? Region, string? PostalCode, string? CountryCode);
