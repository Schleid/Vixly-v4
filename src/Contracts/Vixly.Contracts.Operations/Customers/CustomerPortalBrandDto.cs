namespace Vixly.Contracts.Operations.Customers;

// =========================================================
// FILE: CustomerPortalBrandDto.cs
//
// PURPOSE:
// The tenant's published look for the customer portal (Slice 2).
//
// RESPONSIBILITIES:
// - Carry the values between Customer.Web and Operations.Api
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// =========================================================
public sealed record CustomerPortalBrandDto(string DisplayName, Guid? LogoImageId, Guid? FaviconImageId, string PrimaryColour, string AccentColour);
