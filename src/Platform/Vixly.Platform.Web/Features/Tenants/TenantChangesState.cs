using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Tenants;

// =========================================================
// FILE: TenantChangesState.cs
//
// PURPOSE:
// State of the company change queue (B12).
//
// RESPONSIBILITIES:
// - Hold one page of requests
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// =========================================================
public sealed record TenantChangesState(IReadOnlyList<PlatformTenantChangeItem> Items, int Page, int TotalPages, int TotalCount, bool Loaded, Error? Error)
{
    public static TenantChangesState Initial { get; } = new(Array.Empty<PlatformTenantChangeItem>(), 1, 0, 0, false, null);
}
