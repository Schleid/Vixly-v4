using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformTenantCompanyItem.cs
//
// PURPOSE:
// A company’s legal details as screens and the API show them.
//
// RESPONSIBILITIES:
// - Flatten the company details for reading
//
// IMPORTANT NOTES:
// - Read model; no behaviour
// =========================================================
public sealed record PlatformTenantCompanyItem(
    string LegalName,
    string? TradingName,
    string Email,
    string Phone,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string? StateProvince,
    string? PostalCode,
    string CountryCode)
{
    public static PlatformTenantCompanyItem From(PlatformCompanyDetails company)
    {
        ArgumentNullException.ThrowIfNull(company);

        return new PlatformTenantCompanyItem(
            company.LegalName,
            company.TradingName,
            company.Email.Value,
            company.Phone.Value,
            company.Address.Line1,
            company.Address.Line2,
            company.Address.City,
            company.Address.StateProvince,
            company.Address.PostalCode,
            company.Address.CountryCode);
    }
}
