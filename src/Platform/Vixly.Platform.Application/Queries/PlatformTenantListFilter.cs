using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformTenantListFilter.cs
//
// PURPOSE:
// Filters of the tenant directory.
//
// RESPONSIBILITIES:
// - Carry read-only data to the console
//
// IMPORTANT NOTES:
// - Search covers name, code, slug, email and phone
// =========================================================
public sealed record PlatformTenantListFilter(
    string? Search,
    PlatformTenantStatus? Status,
    string? Tag,
    Guid? AccountManagerUserId);
