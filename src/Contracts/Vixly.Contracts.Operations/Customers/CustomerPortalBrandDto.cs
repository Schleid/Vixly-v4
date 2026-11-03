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
// - IdleMinutes and IdleWarnMinutes: the company's inactivity sign-out for customers and the warning before it (owner, 2026-10-27)
// =========================================================
public sealed record CustomerPortalBrandDto(string DisplayName, Guid? LogoImageId, Guid? FaviconImageId, string PrimaryColour, string AccentColour,
    int IdleMinutes = 60, int IdleWarnMinutes = 2);
