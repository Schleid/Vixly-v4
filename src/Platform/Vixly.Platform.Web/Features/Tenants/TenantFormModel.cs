using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.UseCases.Tenants;

namespace Vixly.Platform.Web.Features.Tenants;

// =========================================================
// FILE: TenantFormModel.cs
//
// PURPOSE:
// The editable fields of a tenant form, bound by the components.
//
// RESPONSIBILITIES:
// - Hold company, owner, language, time zone and slug while typing
// - Build the Application inputs
//
// IMPORTANT NOTES:
// - Plain mutable fields: validation is the domain's job
// - Owner and slug are used by the create form only
// =========================================================
public sealed class TenantFormModel
{
    public string LegalName { get; set; } = string.Empty;

    public string? TradingName { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string AddressLine1 { get; set; } = string.Empty;

    public string? AddressLine2 { get; set; }

    public string City { get; set; } = string.Empty;

    public string? StateProvince { get; set; }

    public string? PostalCode { get; set; }

    public string CountryCode { get; set; } = string.Empty;

    public string Language { get; set; } = "en";

    public string TimeZone { get; set; } = "UTC";

    public string Slug { get; set; } = string.Empty;

    public string OwnerName { get; set; } = string.Empty;

    public string OwnerEmail { get; set; } = string.Empty;

    public string? OwnerPhone { get; set; }

    public static TenantFormModel From(PlatformTenantDetail tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        return new TenantFormModel
        {
            LegalName = tenant.LegalName,
            TradingName = tenant.TradingName,
            Email = tenant.CompanyEmail,
            Phone = tenant.CompanyPhone,
            AddressLine1 = tenant.AddressLine1,
            AddressLine2 = tenant.AddressLine2,
            City = tenant.City,
            StateProvince = tenant.StateProvince,
            PostalCode = tenant.PostalCode,
            CountryCode = tenant.CountryCode,
            Language = tenant.DefaultLanguage,
            TimeZone = tenant.TimeZone,
            Slug = tenant.Slug
        };
    }

    public PlatformCompanyInput ToCompany()
    {
        return new PlatformCompanyInput
        {
            LegalName = LegalName,
            TradingName = TradingName,
            Email = Email,
            Phone = Phone,
            AddressLine1 = AddressLine1,
            AddressLine2 = AddressLine2,
            City = City,
            StateProvince = StateProvince,
            PostalCode = PostalCode,
            CountryCode = CountryCode
        };
    }
}
