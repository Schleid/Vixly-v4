namespace Vixly.Platform.Application.UseCases.Billing.UpdatePlatformCompanyProfile;

// =========================================================
// FILE: UpdatePlatformCompanyProfileRequest.cs
//
// PURPOSE:
// Sets Vixly's company details printed on invoices (D-B12).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class UpdatePlatformCompanyProfileRequest
{
    public string? LegalName { get; init; }

    public string? TradingName { get; init; }

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public string? Line1 { get; init; }

    public string? Line2 { get; init; }

    public string? City { get; init; }

    public string? StateProvince { get; init; }

    public string? PostalCode { get; init; }

    public string? CountryCode { get; init; }

    public string? TaxId { get; init; }

    public string? Website { get; init; }

    public string? SupportEmail { get; init; }

    public string? SupportPhone { get; init; }
}
