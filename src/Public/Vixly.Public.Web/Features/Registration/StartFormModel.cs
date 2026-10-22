namespace Vixly.Public.Web.Features.Registration;

// =========================================================
// FILE: StartFormModel.cs
//
// PURPOSE:
// What the sign-up form posts.
//
// RESPONSIBILITIES:
// - Hold the company, address, owner, portal address and consent
//
// IMPORTANT NOTES:
// - Nothing here is trusted: Platform checks every field (Slice 2)
// =========================================================
public sealed class StartFormModel
{
    public string? LegalName { get; set; }

    public string? TradingName { get; set; }

    public string? CompanyEmail { get; set; }

    public string? CompanyPhone { get; set; }

    public string? AddressLine1 { get; set; }

    public string? AddressLine2 { get; set; }

    public string? City { get; set; }

    public string? StateProvince { get; set; }

    public string? PostalCode { get; set; }

    public string CountryCode { get; set; } = "US";

    public string? OwnerName { get; set; }

    public string? OwnerEmail { get; set; }

    public string? OwnerPhone { get; set; }

    public string? Slug { get; set; }

    public bool AddressConfirmed { get; set; }

    public bool AcceptedTerms { get; set; }

    public string? OfferToken { get; set; }
}
