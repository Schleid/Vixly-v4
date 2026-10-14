using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Tenants;

// =========================================================
// FILE: TenantListState.cs
//
// PURPOSE:
// State of the tenant directory.
//
// RESPONSIBILITIES:
// - Hold one page of tenants and the paging facts
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// =========================================================
public sealed record TenantListState(
    IReadOnlyList<PlatformTenantListItem> Tenants,
    int Page,
    int TotalPages,
    int TotalCount,
    bool CanCreate,
    string BaseDomain,
    bool Loaded,
    Error? Error)
{
    public static TenantListState Initial { get; } = new(Array.Empty<PlatformTenantListItem>(), 1, 0, 0, false, string.Empty, false, null);
}
