namespace Vixly.Operations.Application.Site;

// =========================================================
// FILE: OperationsPublicLocation.cs
//
// PURPOSE:
// A branch or warehouse as visitors see it.
//
// RESPONSIBILITIES:
// - Carry what the site shows
//
// IMPORTANT NOTES:
// - Only active locations marked "show on the site"
// - No phone number: customers call the call centre (country
//   contacts), never a warehouse directly (owner, 2026-10-18)
// =========================================================
public sealed record OperationsPublicLocation(
    Guid Id,
    string Kind,
    string Name,
    string Line1,
    string? Line2,
    string City,
    string? Region,
    string? PostalCode,
    string CountryCode,
    string? OpeningHours,
    bool Receiving,
    bool PickupPoint,
    bool AcceptingDeliveries);
