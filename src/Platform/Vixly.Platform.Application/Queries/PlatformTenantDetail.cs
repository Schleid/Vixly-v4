using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformTenantDetail.cs
//
// PURPOSE:
// Everything the tenant page shows at the top.
//
// RESPONSIBILITIES:
// - Carry read-only data to the console
//
// IMPORTANT NOTES:
// - History and notes are paged separately
// =========================================================
public sealed record PlatformTenantDetail(
    Guid PlatformTenantId,
    string TenantCode,
    string Slug,
    PlatformTenantStatus Status,
    DateTimeOffset StatusChangedUtc,
    DateTimeOffset? ActivatedUtc,
    PlatformTenantOrigin Origin,
    string LegalName,
    string? TradingName,
    string CompanyEmail,
    string CompanyPhone,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string? StateProvince,
    string? PostalCode,
    string CountryCode,
    string DefaultLanguage,
    string TimeZone,
    Guid? AccountManagerUserId,
    string? AccountManagerName,
    bool AccountManagerIsActive,
    IReadOnlyList<string> Tags,
    IReadOnlyList<PlatformTenantContactItem> Contacts,
    PlatformTenantSuspensionItem? OpenSuspension,
    DateTimeOffset CreatedUtc);
