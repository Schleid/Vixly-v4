using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.UseCases.TenantAccount;

// =========================================================
// FILE: PlatformTenantAccountSummary.cs
//
// PURPOSE:
// The tenant's own account: code, names, address, contacts, language, time zone, status and Vixly support (calls 15, 17, 18).
//
// RESPONSIBILITIES:
// - Carry what Operations shows the tenant
//
// IMPORTANT NOTES:
// - Never carries staff-only facts: notes, tags, account manager, tax exemption
// =========================================================
public sealed record PlatformTenantAccountSummary(
    Guid PlatformTenantId,
    string TenantCode,
    string Slug,
    string LegalName,
    string? TradingName,
    string Email,
    string Phone,
    PlatformTenantAddressItem Address,
    string DefaultLanguage,
    string TimeZone,
    PlatformTenantStatus Status,
    IReadOnlyList<PlatformTenantAccountContact> Contacts,
    PlatformVixlySupport? Support)
{
    public static PlatformTenantAccountSummary From(PlatformTenant tenant, PlatformCompanyProfile? vixly)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        PlatformPostalAddress address = tenant.Company.Address;

        return new PlatformTenantAccountSummary(
            tenant.Id,
            tenant.TenantCode,
            tenant.CurrentSlug,
            tenant.Company.LegalName,
            tenant.Company.TradingName,
            tenant.Company.Email.Value,
            tenant.Company.Phone.Value,
            new PlatformTenantAddressItem(address.Line1, address.Line2, address.City, address.StateProvince, address.PostalCode, address.CountryCode),
            tenant.DefaultLanguage.Value,
            tenant.TimeZone,
            tenant.Status,
            tenant.Contacts
                .OrderBy(contact => contact.Role)
                .Select(contact => new PlatformTenantAccountContact(contact.Role, contact.Name, contact.Email.Value, contact.Phone?.Value))
                .ToArray(),
            vixly is null
                ? null
                : new PlatformVixlySupport(
                    vixly.Company.TradingName ?? vixly.Company.LegalName,
                    vixly.SupportEmail ?? vixly.Company.Email.Value,
                    vixly.SupportPhone ?? vixly.Company.Phone.Value,
                    vixly.Website));
    }
}
