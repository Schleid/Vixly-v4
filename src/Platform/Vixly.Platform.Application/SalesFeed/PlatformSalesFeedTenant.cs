using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.SalesFeed;

// =========================================================
// FILE: PlatformSalesFeedTenant.cs
//
// PURPOSE:
// The tenant as every sale event names it (contract version 1).
//
// RESPONSIBILITIES:
// - Take the id, code, names, owner and billing email, country and phone from the tenant
//
// IMPORTANT NOTES:
// - Phone is sent only in E.164 form (a leading "+"); otherwise it is left out (optional field)
// - Website is not recorded in Vixly, so it is never sent (optional field)
// =========================================================
public sealed record PlatformSalesFeedTenant(
    Guid Id,
    string Code,
    string LegalName,
    string DisplayName,
    string OwnerEmail,
    string BillingEmail,
    string Country,
    string? Phone)
{
    public static PlatformSalesFeedTenant From(PlatformTenant tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        string phone = tenant.Company.Phone.Value;

        return new PlatformSalesFeedTenant(
            tenant.Id,
            tenant.TenantCode,
            tenant.Company.LegalName,
            tenant.Company.DisplayName,
            tenant.ContactFor(PlatformTenantContactRole.Owner).Email.Value,
            tenant.ContactFor(PlatformTenantContactRole.Billing).Email.Value,
            tenant.Company.Address.CountryCode,
            phone.StartsWith('+') ? phone : null);
    }
}
