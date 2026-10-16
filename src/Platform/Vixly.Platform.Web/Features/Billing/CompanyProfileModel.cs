using Vixly.Platform.Application.UseCases.Billing;

namespace Vixly.Platform.Web.Features.Billing;

// =========================================================
// FILE: CompanyProfileModel.cs
//
// PURPOSE:
// The editable copy of the company profile printed on billing documents (D-B12).
//
// RESPONSIBILITIES:
// - Copy the profile in; empty when it was never filled
//
// IMPORTANT NOTES:
// - Mutable form model; the domain validates
// =========================================================
public sealed class CompanyProfileModel
{
    public string? LegalName { get; set; }

    public string? TradingName { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Line1 { get; set; }

    public string? Line2 { get; set; }

    public string? City { get; set; }

    public string? StateProvince { get; set; }

    public string? PostalCode { get; set; }

    public string? CountryCode { get; set; }

    public string? TaxId { get; set; }

    public string? Website { get; set; }

    public string? SupportEmail { get; set; }

    public string? SupportPhone { get; set; }

    public static CompanyProfileModel From(PlatformCompanyProfileItem? profile)
    {
        return profile is null
            ? new CompanyProfileModel()
            : new CompanyProfileModel
            {
                LegalName = profile.LegalName,
                TradingName = profile.TradingName,
                Email = profile.Email,
                Phone = profile.Phone,
                Line1 = profile.Line1,
                Line2 = profile.Line2,
                City = profile.City,
                StateProvince = profile.StateProvince,
                PostalCode = profile.PostalCode,
                CountryCode = profile.CountryCode,
                TaxId = profile.TaxId,
                Website = profile.Website,
                SupportEmail = profile.SupportEmail,
                SupportPhone = profile.SupportPhone
            };
    }
}
