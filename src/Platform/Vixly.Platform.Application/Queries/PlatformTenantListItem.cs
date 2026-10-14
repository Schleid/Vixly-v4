using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformTenantListItem.cs
//
// PURPOSE:
// One row of the tenant directory.
//
// RESPONSIBILITIES:
// - Carry read-only data to the console
//
// IMPORTANT NOTES:
// - Only the columns the list shows (P-1)
// =========================================================
public sealed record PlatformTenantListItem(
    Guid PlatformTenantId,
    string TenantCode,
    string DisplayName,
    string Slug,
    PlatformTenantStatus Status,
    string CountryCode,
    string? AccountManagerName,
    bool AccountManagerIsActive,
    IReadOnlyList<string> Tags,
    DateTimeOffset CreatedUtc);
